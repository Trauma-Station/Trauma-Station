// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Cloning;
using Content.Shared.Body;
using Content.Shared.Mind.Components;
using Content.Shared.Polymorph;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Language.Systems;
using Content.Trauma.Shared.Mobs;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// This handles all knowledge related entities.
/// </summary>
public abstract partial class SharedKnowledgeSystem : CommonKnowledgeSystem
{
    [Dependency] protected readonly IConfigurationManager _cfg = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager _proto = default!;
    [Dependency] protected readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly EntityQuery<AwakeMobComponent> _awakeQuery = default!;
    [Dependency] private readonly EntityQuery<SkillComponent> _skillQuery = default!;
    [Dependency] private readonly EntityQuery<AttributeComponent> _attributeQuery = default!;
    [Dependency] private readonly EntityQuery<ProficiencyComponent> _proficiencyQuery = default!;
    [Dependency] private readonly EntityQuery<TalentComponent> _talentQuery = default!;
    [Dependency] private readonly EntityQuery<KnowledgeContainerComponent> _containerQuery = default!;
    [Dependency] private readonly EntityQuery<KnowledgeHolderComponent> _holderQuery = default!;

    /// <summary>
    /// Every skill prototype and its data.
    /// </summary>
    public Dictionary<EntProtoId, SkillComponent> AllSkills = new();

    /// <summary>
    /// Every attribute prototype and its data.
    /// </summary>
    public Dictionary<EntProtoId, AttributeComponent> AllAttributes = new();
    public static readonly LocId[] MasteryNames = [
        "unskilled",
        "novice",
        "average",
        "advanced",
        "expert",
        "master"
    ];

    private bool _skillGain;
    private TimeSpan _nextUpdate;
    private TimeSpan _updateDelay = TimeSpan.FromSeconds(1);
    private float _learnChance = 0.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeLanguage();
        InitializeMartialArts();
        InitializeOnWear();

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentStartup>(OnContainerStartup);
        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentShutdown>(OnContainerShutdown);
        SubscribeLocalEvent<KnowledgeContainerComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<KnowledgeContainerComponent, BorgBrainInsertedEvent>(OnBorgBrainInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, BorgBrainRemovedEvent>(OnBorgBrainRemoved);
        SubscribeLocalEvent<KnowledgeContainerComponent, TransferredToCloneEvent>(OnCloneTransfer);

        SubscribeLocalEvent<KnowledgeHolderComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<KnowledgeHolderComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        Subs.CVar(_cfg, TraumaCVars.SkillGain, x => _skillGain = x, true);

        LoadPrototypes();
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_skillGain || _timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + _updateDelay;

        // client only predicts rolling for itself
        if (_player.LocalEntity is { } player)
        {
            UpdateSkills(player);
            return;
        }

        var query = EntityQueryEnumerator<KnowledgeHolderComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            UpdateSkills(ent);
        }
    }

    private void UpdateSkills(EntityUid ent)
    {
        if (TryGetAllSkillUnits(ent) is not { } knowledgeUnits)
            return;

        foreach (var knowledgeUnit in knowledgeUnits)
        {
            if (RollForLevelUp(knowledgeUnit, ent))
                return;
        }
    }

    private void OnContainerStartup(Entity<KnowledgeContainerComponent> ent, ref ComponentStartup args)
    {
        EnsureContainer(ent);
    }

    private void OnContainerShutdown(Entity<KnowledgeContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Container is { } container)
            _container.ShutdownContainer(container);
    }

    protected void LinkContainer(EntityUid target, Entity<KnowledgeContainerComponent> ent)
    {
        // its all networked
        if (_timing.ApplyingState)
            return;

        var holder = EnsureComp<KnowledgeHolderComponent>(target);
        if (holder.KnowledgeEntity == ent.Owner)
            return; // no change

        DebugTools.Assert(ent.Comp.Holder == null,
            $"Tried to link {ToPrettyString(target)} to {ToPrettyString(ent)} but it was already linked to another holder {ToPrettyString(ent.Comp.Holder)}!");
        DebugTools.Assert(holder.KnowledgeEntity == null,
            $"Tried to link {ToPrettyString(target)} to {ToPrettyString(ent)} but it was already linked to another container {ToPrettyString(holder.KnowledgeEntity)}!");

        holder.KnowledgeEntity = ent;
        Dirty(target, holder);
        ent.Comp.Holder = target;
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.Holder));
    }

    private void UnlinkContainer(EntityUid target, Entity<KnowledgeContainerComponent> ent)
    {
        // its all networked
        if (_timing.ApplyingState ||
            !_holderQuery.TryComp(target, out var holder) ||
            holder.KnowledgeEntity == null) // already unlinked
            return;

        DebugTools.Assert(ent.Comp.Holder == target,
            $"Tried to unlink {ToPrettyString(target)} from {ToPrettyString(ent)} but it was linked to a different holder {ToPrettyString(ent.Comp.Holder)}!");
        DebugTools.Assert(holder.KnowledgeEntity == ent.Owner,
            $"Tried to unlink {ToPrettyString(target)} from {ToPrettyString(ent)} but it was linked to a different container {ToPrettyString(holder.KnowledgeEntity)}!");

        holder.KnowledgeEntity = null;
        Dirty(target, holder);
        ent.Comp.Holder = null;
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.Holder));
    }

    private void OnOrganInserted(Entity<KnowledgeContainerComponent> ent, ref OrganGotInsertedEvent args)
    {
        LinkContainer(args.Target, ent);
    }

    private void OnOrganRemoved(Entity<KnowledgeContainerComponent> ent, ref OrganGotRemovedEvent args)
    {
        UnlinkContainer(args.Target, ent);
    }

    private void OnBorgBrainInserted(Entity<KnowledgeContainerComponent> ent, ref BorgBrainInsertedEvent args)
    {
        LinkContainer(args.Chassis, ent);
    }

    private void OnBorgBrainRemoved(Entity<KnowledgeContainerComponent> ent, ref BorgBrainRemovedEvent args)
    {
        UnlinkContainer(args.Chassis, ent);
    }

    private void OnCloneTransfer(Entity<KnowledgeContainerComponent> ent, ref TransferredToCloneEvent args)
    {
        TransferKnowledge(ent, args.Cloned);
    }

    private void OnPolymorphed(Entity<KnowledgeHolderComponent> ent, ref PolymorphedEvent args)
    {
        if (ent.Owner == args.OldEntity)
            TransferKnowledge(ent, args.NewEntity);
    }

    private void OnMindAdded(Entity<KnowledgeHolderComponent> ent, ref MindAddedMessage args)
    {
        // all player-controlled mobs can use knowledge
        // carps learning how to cook..?
        EnsureKnowledgeContainer(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        LoadSkillPrototypes();
        LoadAttributePrototypes();
    }


    private void LoadSkillPrototypes()
    {
        AllSkills.Clear();
        var name = Factory.GetComponentName<SkillComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            // TODO: replace with TryComp after engine update
            if (!proto.TryGetComponent<SkillComponent>(name, out var comp))
                continue;

            AllSkills[proto.ID] = comp;
        }
    }

    private void LoadAttributePrototypes()
    {
        AllAttributes.Clear();
        var name = Factory.GetComponentName<AttributeComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            // TODO: replace with TryComp after engine update
            if (!proto.TryGetComponent<AttributeComponent>(name, out var comp))
                continue;

            AllAttributes[proto.ID] = comp;
        }
    }

    /// <summary>
    /// Attempts to transfer all knowledge from one entity (container or holder) to another (holder).
    /// </summary>
    public void TransferKnowledge(EntityUid ent, EntityUid otherHolder)
    {
        if (TryGetAllKnowledgeUnits(ent) is not { } found)
            return;

        var mobContainer = EnsureKnowledgeContainer(otherHolder);
        if (mobContainer.Comp.Container is not { } container)
            return;

        foreach (var knowledgeEnt in found)
        {
            _container.Insert(knowledgeEnt, container);
            var protoId = Prototype(knowledgeEnt)?.ID;
            if (protoId is { } id)
                mobContainer.Comp.KnowledgeDict[id] = knowledgeEnt;
        }
        ClearKnowledge(ent, false);
    }

    /// <summary>
    /// Shows a skill popup to a user, respecting the popup cooldown.
    /// Can be called from server, client or both.
    /// Can be hidden by client skill popups setting.
    /// </summary>
    public void SkillPopup(string popup, EntityUid user)
    {
        var ev = new SkillPopupEvent(popup);
        if (_net.IsServer)
            RaiseNetworkEvent(ev, user);
        else if (_player.LocalEntity == user && _timing.IsFirstTimePredicted)
            RaiseLocalEvent(ev);
    }

    public void AddExperience(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int xp, int levelCap = 100, bool popup = true)
    {
        if (!_skillGain)
            return;

        if (GetSkill(ent, id) is not { } unit)
        {
            // if you don't have it, you have a small change to learn it when gaining some xp
            if (SharedRandomExtensions.PredictedProb(_timing, _learnChance, GetNetEntity(ent)))
                EnsureKnowledge<SkillComponent>(ent, id, 0, popup);
            return;
        }

        if (ent.Comp.Holder is { } holder)
        {
            AddExperience(unit.AsNullable(), holder, xp, levelCap);

            var updateEv = new UpdateExperienceEvent();
            RaiseLocalEvent(holder, ref updateEv);
        }
    }

    public void AddExperience(Entity<SkillComponent?> ent, EntityUid target, int added, int limit = 100)
    {
        if (!_skillGain || !_skillQuery.Resolve(ent, ref ent.Comp))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.TimeToNextExperience || ent.Comp.LearnedLevel >= Math.Min(limit, 100))
            return;

        ent.Comp.TimeToNextExperience = now + ent.Comp.TimeBetweenExperience;
        ent.Comp.Experience += added + ent.Comp.BonusExperience;
        Dirty(ent);

        RollForLevelUp((ent, ent.Comp), target);
    }

    /// <summary>
    /// Rolls Levelup. True on roll. False on not.
    /// </summary>
    public bool RollForLevelUp(Entity<SkillComponent> ent, EntityUid target)
    {
        var getMastery = GetMastery(ent.Comp.NetLevel);
        (int, bool) rollResult = (0, false);

        // If we don't have enough experience or level is max, return.
        if (ent.Comp.Experience < ent.Comp.ExperienceCost || ent.Comp.LearnedLevel >= 100)
            return false;

        // This should roll as many times as experience cached experience.
        int timesToRoll = ent.Comp.Experience / ent.Comp.ExperienceCost;
        ent.Comp.Experience -= ent.Comp.ExperienceCost * timesToRoll;
        (int, bool) rollInnard;
        for (int i = 0; i < timesToRoll && ent.Comp.LearnedLevel < 100; i++)
        {
            rollInnard = RollPenetrating(ent);
            rollResult = (rollInnard.Item1, rollInnard.Item2 || rollResult.Item2);
            ent.Comp.LearnedLevel += rollResult.Item1;
        }

        if (ent.Comp.LearnedLevel > 100) // Ensures Level doesn't go above 100.
            ent.Comp.LearnedLevel = 100;

        // Controls client popup whatnot when you level up.
        if (rollResult.Item2)
            SkillPopup(Loc.GetString("knowledge-level-epiphany", ("knowledge", Name(ent))), target);

        if (getMastery != GetMastery(ent.Comp.NetLevel) && !rollResult.Item2)
            SkillPopup(Loc.GetString("knowledge-level-up-popup", ("knowledge", Name(ent)), ("mastery", GetMasteryString(ent).ToLower())), target);
        else if (!rollResult.Item2)
            SkillPopup(Loc.GetString("knowledge-level-more", ("knowledge", Name(ent))), target);

        return true;
    }

    public (ProtoId<SkillCategoryPrototype> Category, SkillInfo Info) GetSkillInfo(Entity<SkillComponent> ent)
    {
        var knowledgeInfo = new SkillInfo("", "", ent.Comp.Color, ent.Comp.Sprite, ent.Comp.LearnedLevel, ent.Comp.NetLevel, ent.Comp.Experience, ent.Comp.ExperienceCost);
        // TODO: make this an event raised on ent
        var name = Name(ent);
        knowledgeInfo.Description = Loc.GetString("knowledge-info-description", ("level", ent.Comp.NetLevel), ("mastery", GetMasteryString(ent)), ("exp", ent.Comp.Experience));
        if (_langQuery.TryComp(ent, out var languageKnowledge))
        {
            var locKey = (languageKnowledge.Speaks, languageKnowledge.Understands) switch
            {
                (true, true) => "knowledge-language-speaks-understands",
                (true, false) => "knowledge-language-speaks",
                _ => "knowledge-language-understands"
            };

            knowledgeInfo.Name = Loc.GetString(locKey, ("language", name));
        }
        else if (TryComp<MartialArtsSkillComponent>(ent, out var martialKnowledge))
        {
            knowledgeInfo.Name = Loc.GetString("knowledge-martial-arts-name", ("name", name));
        }
        else
        {
            knowledgeInfo.Name = name;
        }
        return (ent.Comp.Category, knowledgeInfo);
    }

    public (int Order, AttributeInfo Info) GetAttributeInfo(Entity<AttributeComponent> ent)
    {
        var info = new AttributeInfo();

        info.Name = Name(ent);
        info.Description = Description(ent);
        info.Inherent = ent.Comp.Inherent;
        return (ent.Comp.Order, info);
    }

    /// <summary>
    /// Increase a knowledge unit's level for a target entity.
    /// This sets the level to max(current, new), NOT adding.
    /// If it does not exist it will be created.
    /// </summary>
    /// <returns>
    /// Null if spawning it fails.
    /// </returns>
    public Entity<T>? EnsureKnowledge<T>(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int level = 0, bool popup = true) where T : IComponent
    {
        if (EnsureKnowledge(ent, id, level, popup) is { } unit && TryComp<T>(unit, out var comp))
            return (unit, comp);
        return null;
    }

    public EntityUid? EnsureKnowledge(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int level = 0, bool popup = true)
    {
        // Checkslop
        if (GetSkill(ent, id) is { } existing)
        {
            if (existing.Comp.LearnedLevel < level)
            {
                existing.Comp.LearnedLevel = level;
                Dirty(existing, existing.Comp);
            }
            return existing.Owner;
        }
        else if (GetAttribute(ent, id) is { } attribute)
        {
            if (attribute.Comp.Inherent < level)
            {
                attribute.Comp.Inherent = level;
                Dirty(attribute, attribute.Comp);
            }
            return attribute.Owner;
        }
        else if (GetProficiency(ent, id) is { } proficiency)
        {
            return proficiency.Owner;
        }

        else if (GetTalent(ent, id) is { } talent)
        {
            if (talent.Comp.Level < level)
            {
                talent.Comp.Level = level;
                Dirty(talent, talent.Comp);
            }
            return talent.Owner;
        }
        // Checkslop

        PredictedTrySpawnInContainer(id, ent.Owner, KnowledgeContainerComponent.ContainerId, out var spawned);
        if (spawned is not { } unit)
        {
            Log.Error($"Failed to spawn knowledge {id} for {ToPrettyString(ent)}!");
            return null;
        }

        // Checkslop
        if (_skillQuery.TryComp(unit, out var comp))
        {
            comp.LearnedLevel = level;
            Dirty(unit, comp);
        }
        else if (_attributeQuery.TryComp(unit, out var attribute))
        {
            attribute.Inherent = level;
            Dirty(unit, attribute);
        }
        else if (_proficiencyQuery.TryComp(unit, out var proficiency))
        {

        }
        else if (_talentQuery.TryComp(unit, out var talent))
        {
            talent.Level = level;
            Dirty(unit, talent);
        }

        ent.Comp.KnowledgeDict[id] = unit;
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.KnowledgeDict));

        if (ent.Comp.Holder is not { } holder)
            return unit; // added knowledge to a loose brain...
        // Checkslop

        var ev = new KnowledgeAddedEvent(ent, holder);
        RaiseLocalEvent(unit, ref ev);

        if (popup)
        {
            var msg = Loc.GetString("knowledge-unit-learned-popup", ("knowledge", Name(unit)));
            SkillPopup(msg, holder);
        }
        return unit;
    }

    /// <summary>
    /// Raises a skill's mastery level by some number.
    /// Adds the skill if it's missing.
    /// </summary>
    public Entity<SkillComponent>? RaiseMastery(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int mastery, bool popup = true)
    {
        if (EnsureKnowledge<SkillComponent>(ent, id, popup: popup) is not { } unit)
            return null;

        mastery += GetMastery(unit.Comp.LearnedLevel);
        var level = GetInverseMastery(mastery);
        unit.Comp.LearnedLevel = Math.Min(level, 100);
        Dirty(unit);
        return unit;
    }

    /// <summary>
    /// Raises a skill's mastery by rolls.
    /// Adds skill if missing.
    /// </summary>
    public Entity<SkillComponent>? RaiseSkillByRolls(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int rolls, bool popup = true)
    {
        if (EnsureKnowledge<SkillComponent>(ent, id, popup: popup) is not { } unit)
            return null;

        AddExperience(ent, id, unit.Comp.ExperienceCost * rolls);
        RollForLevelUp(unit, ent);
        return unit;
    }

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public void AddKnowledgeUnits(EntityUid target, Dictionary<EntProtoId, int> knowledgeList, bool popup = false)
    {
        if (GetContainer(target) is not { } ent)
            return;

        foreach (var (id, level) in knowledgeList)
        {
            EnsureKnowledge<SkillComponent>(ent, id, level, popup);
        }

        var updateEv = new UpdateExperienceEvent();
        RaiseLocalEvent(target, ref updateEv);
    }

    /// <summary>
    /// Removes a knowledge unit from a container. Will not remove a knowledge unit if it's marked as unremoveable,
    /// unless force parameter is true.
    /// </summary>
    public EntityUid? RemoveKnowledge(EntityUid target, [ForbidLiteral] EntProtoId id, bool force = false)
    {
        if (GetContainer(target) is not { } ent ||
            ent.Comp.Holder is not { } holder ||
            GetSkill(ent, id) is not { } unit ||
            unit.Comp.Unremoveable && !force)
            return null;

        ent.Comp.KnowledgeDict.Remove(id);
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.KnowledgeDict));

        var ev = new KnowledgeRemovedEvent(ent, holder);
        RaiseLocalEvent(unit, ref ev);

        PredictedQueueDel(unit);

        SkillPopup(Loc.GetString("knowledge-unit-forgotten-popup", ("knowledge", Name(unit))), holder);
        return target;
    }

    /// <summary>
    /// Gets a skill unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not a knowledge container, or if a skill unit wasn't found.
    /// </returns>
    public override Entity<SkillComponent>? GetSkill(EntityUid target, [ForbidLiteral] EntProtoId id)
        => GetContainer(target) is { } ent
            ? GetSkill(ent, id)
            : null;

    public Entity<SkillComponent>? GetSkill(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id)
        => ent.Comp.KnowledgeDict.TryGetValue(id, out var unit) && _skillQuery.TryComp(unit, out var comp)
            ? (unit, comp)
            : null;

    /// <summary>
    /// Gets an attribute unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not a knowledge container, or if an attribute unit wasn't found.
    /// </returns>
    public override Entity<AttributeComponent>? GetAttribute(EntityUid target, [ForbidLiteral] EntProtoId id)
        => GetContainer(target) is { } ent
            ? GetAttribute(ent, id)
            : null;

    public Entity<AttributeComponent>? GetAttribute(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id)
        => ent.Comp.KnowledgeDict.TryGetValue(id, out var unit) && _attributeQuery.TryComp(unit, out var comp)
            ? (unit, comp)
            : null;

    /// <summary>
    /// Gets a proficiency unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not a knowledge container, or if an attribute unit wasn't found.
    /// </returns>
    public override Entity<ProficiencyComponent>? GetProficiency(EntityUid target, [ForbidLiteral] EntProtoId id)
        => GetContainer(target) is { } ent
            ? GetProficiency(ent, id)
            : null;

    public Entity<ProficiencyComponent>? GetProficiency(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id)
        => ent.Comp.KnowledgeDict.TryGetValue(id, out var unit) && _proficiencyQuery.TryComp(unit, out var comp)
            ? (unit, comp)
            : null;

    /// <summary>
    /// Gets a proficiency unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not a knowledge container, or if an attribute unit wasn't found.
    /// </returns>
    public override Entity<TalentComponent>? GetTalent(EntityUid target, [ForbidLiteral] EntProtoId id)
        => GetContainer(target) is { } ent
            ? GetTalent(ent, id)
            : null;

    public Entity<TalentComponent>? GetTalent(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id)
        => ent.Comp.KnowledgeDict.TryGetValue(id, out var unit) && _talentQuery.TryComp(unit, out var comp)
            ? (unit, comp)
            : null;

    /// <summary>
    /// Returns all skill units inside the container component.
    /// </summary>
    public List<Entity<SkillComponent>>? TryGetAllSkillUnits(EntityUid target)
    {
        if (GetContainer(target) is not { } ent)
            return null;

        var found = new List<Entity<SkillComponent>>();
        foreach (var unit in ent.Comp.KnowledgeDict.Values)
        {
            if (_skillQuery.TryComp(unit, out var comp))
                found.Add((unit, comp));
        }

        return found;
    }

    /// <summary>
    /// Returns all skill units inside the container component.
    /// </summary>
    public List<Entity<AttributeComponent>>? TryGetAllAttributeUnits(EntityUid target)
    {
        if (GetContainer(target) is not { } ent)
            return null;

        var found = new List<Entity<AttributeComponent>>();
        foreach (var unit in ent.Comp.KnowledgeDict.Values)
        {
            if (_attributeQuery.TryComp(unit, out var comp))
                found.Add((unit, comp));
        }

        return found;
    }

    /// <summary>
    /// Returns all proficiency units inside the container component.
    /// </summary>
    public List<Entity<ProficiencyComponent>>? TryGetAllProficiencyUnits(EntityUid target)
    {
        if (GetContainer(target) is not { } ent)
            return null;

        var found = new List<Entity<ProficiencyComponent>>();
        foreach (var unit in ent.Comp.KnowledgeDict.Values)
        {
            if (_proficiencyQuery.TryComp(unit, out var comp))
                found.Add((unit, comp));
        }

        return found;
    }

    /// <summary>
    /// Returns all talent units inside the container component.
    /// </summary>
    public List<Entity<TalentComponent>>? TryGetAllTalentUnits(EntityUid target)
    {
        if (GetContainer(target) is not { } ent)
            return null;

        var found = new List<Entity<TalentComponent>>();
        foreach (var unit in ent.Comp.KnowledgeDict.Values)
        {
            if (_talentQuery.TryComp(unit, out var comp))
                found.Add((unit, comp));
        }

        return found;
    }

    /// <summary>
    /// Returns all Knowledge units inside the container component.
    /// </summary>
    public List<EntityUid>? TryGetAllKnowledgeUnits(EntityUid target)
    {
        if (GetContainer(target) is not { } ent)
            return null;

        var found = new List<EntityUid>();
        foreach (var unit in ent.Comp.KnowledgeDict.Values)
        {
            found.Add(unit);
        }

        return found;
    }


    /// <summary>
    /// Returns the first knowledge entity of the target that has a given component.
    /// </summary>
    public EntityUid? HasKnowledgeComp<T>(EntityUid target) where T : IComponent
    {
        if (GetContainer(target)?.Comp.Container is not { } container)
            return null;

        var query = GetEntityQuery<T>();
        foreach (var knowledge in container.ContainedEntities)
        {
            if (query.HasComp(knowledge))
                return target;
        }

        return null;
    }

    /// <summary>
    /// Returns all knowledge entities that have a required component.
    /// </summary>
    public List<Entity<T, SkillComponent>>? GetSkillWith<T>(EntityUid target) where T : IComponent
    {
        if (GetContainer(target)?.Comp.Container is not { } container)
            return null;

        var knowledgeEnts = new List<Entity<T, SkillComponent>>();
        var query = GetEntityQuery<T>();
        foreach (var knowledge in container.ContainedEntities)
        {
            if (!_skillQuery.TryComp(knowledge, out var knowledgeComp))
                continue;

            if (query.TryComp(knowledge, out var comp))
                knowledgeEnts.Add((knowledge, comp, knowledgeComp));
        }

        return knowledgeEnts;
    }

    /// <summary>
    /// Returns true if an entity is a knowldge holder, regardless of having a container set.
    /// </summary>
    public bool IsHolder(EntityUid target)
        => _holderQuery.HasComp(target);

    public override void ClearKnowledge(EntityUid target, bool deleteAll)
    {
        if (GetContainer(target) is not { } ent)
            return;

        ent.Comp.KnowledgeDict.Clear();
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.KnowledgeDict));
        ChangeMartialArts(ent, target, null);
        ChangeLanguage(ent, null);
        if (deleteAll && ent.Comp.Container is { } container)
        {
            foreach (var entity in container.ContainedEntities)
            {
                PredictedQueueDel(entity);
            }
        }
    }

    /// <summary>
    /// Get the knowledge container (brain) of a potential knowledge holder (mob, borg, etc or a brain)
    /// </summary>
    public Entity<KnowledgeContainerComponent>? GetContainer(EntityUid uid)
    {
        // if called with a brain, return itself
        if (_containerQuery.TryComp(uid, out var comp))
            return (uid, comp);

        // otherwise try use the cached brain
        if (_holderQuery.CompOrNull(uid)?.KnowledgeEntity is not { } ent || TerminatingOrDeleted(ent))
            return null;

        if (_containerQuery.TryComp(ent, out var container))
            return (ent, container);

        Log.Error($"Knowledge entity {ToPrettyString(ent)} of holder {ToPrettyString(uid)} did not have KnowledgeContainerComponent!");
        return null;
    }

    /// <summary>
    /// Relays an event to all knowledge entities a mob has.
    /// Does nothing if the mob is asleep or crit/dead.
    /// </summary>
    public void RelayEvent<T>(Entity<KnowledgeHolderComponent> ent, ref T args) where T : notnull
    {
        if (!_awakeQuery.HasComp(ent) || GetContainer(ent)?.Comp.Container is not { } container)
            return;

        foreach (var unit in container.ContainedEntities)
        {
            RaiseLocalEvent(unit, ref args);
        }
    }

    /// <summary>
    /// Relays an event to all non-martial arts knowledges a mob has.
    /// It also relays it to the active martial art, but not any inactive oens.
    /// </summary>
    public void RelayActiveEvent<T>(Entity<KnowledgeHolderComponent> ent, ref T args) where T : notnull
    {
        if (!_awakeQuery.HasComp(ent) || GetContainer(ent) is not { } brain || brain.Comp.Container is not { } container)
            return;

        foreach (var unit in container.ContainedEntities)
        {
            // dont relay to inactive martial arts
            if (_artQuery.HasComp(unit) && unit != brain.Comp.ActiveMartialArt)
                continue;

            RaiseLocalEvent(unit, ref args);
        }
    }

    public void RelayMartialArt<T>(Entity<KnowledgeHolderComponent> ent, ref T args) where T : notnull
    {
        if (_awakeQuery.HasComp(ent) && GetActiveMartialArt(ent) is { } skill)
            RaiseLocalEvent(skill, ref args);
    }

    public override Dictionary<EntProtoId, int> GetSkillMasteries(EntityUid target)
    {
        var skills = new Dictionary<EntProtoId, int>();
        if (GetContainer(target) is not { } brain)
            return skills;

        foreach (var (id, unit) in brain.Comp.KnowledgeDict)
        {
            skills[id] = GetMastery(unit);
        }
        return skills;
    }

    public string GetMasteryString(Entity<SkillComponent> ent)
        => GetMasteryString(GetMastery(ent.Comp.NetLevel));

    public override string GetMasteryString(int mastery)
        => Loc.GetString("knowledge-mastery-" + MasteryNames[Math.Clamp(mastery, 0, 5)]);

    public override int GetMastery(int level)
        => level switch
        {
            >= 100 => 6, // 6th mastery doesn't exist, but we can use this to say max level
            >= 88 => 5,
            >= 76 => 4,
            >= 51 => 3,
            >= 26 => 2,
            >= 1 => 1,
            _ => 0,
        };

    public override int GetMastery(EntityUid uid)
        => GetMastery(GetLevel(uid));

    /// <summary>
    /// Get the level of a knowledge entity, defaulting to 0 for bad entities.
    /// Applies temporary levels from e.g. equipment.
    /// </summary>
    public int GetLevel(EntityUid uid)
        => _skillQuery.TryComp(uid, out var comp)
            ? Math.Clamp(comp.NetLevel, 0, 100)
            : 0;

    public override int GetInverseMastery(int mastery)
        => mastery switch
        {
            >= 6 => 100, // 6th mastery doesn't exist, but we can use this to say max level
            >= 5 => 88,
            >= 4 => 76,
            >= 3 => 51,
            >= 2 => 26,
            >= 1 => 1,
            _ => 0,
        };

    private int DiceDictionary(Entity<SkillComponent> ent, int shift = 0)
    {
        return (GetMastery(ent.Comp) + shift) switch
        {
            >= 5 => 3,
            >= 4 => 4,
            >= 3 => 6,
            >= 2 => 8,
            >= 1 => 12,
            _ => 12,
        };
    }

    public override float SharpCurve(Entity<SkillComponent> knowledge, int offset = 0, float inverseScale = 100.0f)
        => SharpCurve(knowledge.Comp.NetLevel, offset, inverseScale);

    public float SharpCurve(int level, int offset = 0, float inverseScale = 100f)
    {
        // ((level + offset)/inverseScale)^2
        // for level: [0, 100] and inverseScale = 100, this is just the graph of x^2 on [0, 1] :)
        var linear = (float) (level + offset) / inverseScale;
        return linear * linear;
    }

    public (int, bool) RollPenetrating(Entity<SkillComponent> ent)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner));
        var sides = DiceDictionary(ent);
        var isCritical = false;
        int penetratingRolls = 0;
        int currentRoll = rand.Next(1, sides + 1);
        int total = currentRoll;

        while (currentRoll == sides && penetratingRolls < 10)
        {
            sides = DiceDictionary(ent, penetratingRolls / 2);
            currentRoll = rand.Next(1, sides + 1);
            total += currentRoll - 1;
            isCritical = true;
            penetratingRolls++;
        }

        return (total, isCritical);
    }

    private Container EnsureContainer(Entity<KnowledgeContainerComponent> ent)
    {
        if (ent.Comp.Container != null)
            return ent.Comp.Container;

        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, KnowledgeContainerComponent.ContainerId);
        return ent.Comp.Container;
    }

    public Entity<KnowledgeContainerComponent> EnsureKnowledgeContainer(EntityUid uid)
    {
        EnsureComp<KnowledgeHolderComponent>(uid);
        if (GetContainer(uid) is { } brain)
            return brain;

        // if there's no brain store knowledge on the mob itself
        var comp = EnsureComp<KnowledgeContainerComponent>(uid);
        LinkContainer(uid, (uid, comp));
        return (uid, comp);
    }
}

/// <summary>
/// Raised on a knowledge entity after it gets added to a container.
/// </summary>
[ByRefEvent]
public record struct KnowledgeAddedEvent(Entity<KnowledgeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Raised on a knowledge entity after it has been removed from a container, before deleting it.
/// </summary>
[ByRefEvent]
public record struct KnowledgeRemovedEvent(Entity<KnowledgeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Raised on an active knowledge entity just before deactivating it.
/// </summary>
[ByRefEvent]
public record struct KnowledgeEnabledEvent(Entity<KnowledgeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Raised on an active knowledge entity just after activating it.
/// </summary>
[ByRefEvent]
public record struct KnowledgeDisabledEvent(Entity<KnowledgeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Event to try show a skill popup to the user.
/// Both networked and raised locally if predicted.
/// </summary>
[Serializable, NetSerializable]
public sealed class SkillPopupEvent(string popup) : EntityEventArgs
{
    public readonly string Popup = popup;
}
