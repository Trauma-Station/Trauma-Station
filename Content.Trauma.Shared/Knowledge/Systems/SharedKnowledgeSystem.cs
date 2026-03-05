// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Construction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Common.Silicons.Borgs;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// This handles all knowledge related entities.
/// </summary>
public abstract partial class SharedKnowledgeSystem : CommonKnowledgeSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<KnowledgeComponent> _query;
    private EntityQuery<KnowledgeContainerComponent> _containerQuery;
    private EntityQuery<KnowledgeHolderComponent> _holderQuery;

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
        InitializeConstruction();
        InitializeQuality();
        InitializeShooting();

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentStartup>(OnContainerStartup);
        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentShutdown>(OnContainerShutdown);
        SubscribeLocalEvent<KnowledgeContainerComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<KnowledgeContainerComponent, BorgBrainInsertedEvent>(OnBorgBrainInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, BorgBrainRemovedEvent>(OnBorgBrainRemoved);

        SubscribeLocalEvent<KnowledgeHolderComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<KnowledgeHolderComponent, AddExperienceEvent>(OnAddExperience);

        _query = GetEntityQuery<KnowledgeComponent>();
        _containerQuery = GetEntityQuery<KnowledgeContainerComponent>();
        _holderQuery = GetEntityQuery<KnowledgeHolderComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + _updateDelay;

        var query = EntityQueryEnumerator<KnowledgeHolderComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            if (TryGetAllKnowledgeUnits(ent) is not { } knowledgeUnits)
                continue;

            foreach (var knowledgeUnit in knowledgeUnits)
            {
                if (RollForLevelUp(knowledgeUnit, ent))
                    break;
            }
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

    private void OnMindAdded(Entity<KnowledgeHolderComponent> ent, ref MindAddedMessage args)
    {
        // all player-controlled mobs can use knowledge
        // carps learning how to cook..?
        EnsureKnowledgeContainer(ent);
    }

    private void OnAddExperience(Entity<KnowledgeHolderComponent> ent, ref AddExperienceEvent args)
    {
        if (GetContainer(ent) is not {} brain)
            return;

        AddExperience(brain, args.KnowledgeType, args.Experience, popup: args.Popup);
    }

    public void AddExperience(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int xp, bool popup = true)
    {
        if (GetKnowledge(ent, id) is not {} unit)
        {
            // if you don't have it, you have a small change to learn it when gaining some xp
            if (SharedRandomExtensions.PredictedProb(_timing, _learnChance, GetNetEntity(ent)))
                EnsureKnowledge(ent, id, 0, popup);
            return;
        }

        if (ent.Comp.Holder is {} holder)
        {
            AddExperience(unit, holder, xp);

            var updateEv = new UpdateExperienceEvent();
            RaiseLocalEvent(holder, ref updateEv);
        }
    }

    public void AddExperience(Entity<KnowledgeComponent> ent, EntityUid target, int added)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.TimeToNextExperience)
            return;

        ent.Comp.TimeToNextExperience = now + TimeSpan.FromSeconds(1);
        ent.Comp.Experience += added + ent.Comp.BonusExperience;
        Dirty(ent);

        RollForLevelUp(ent, target);
    }

    /// <summary>
    /// Rolls Levelup. True on roll. False on not.
    /// </summary>
    public bool RollForLevelUp(Entity<KnowledgeComponent> ent, EntityUid target)
    {
        var getMastery = GetMastery(ent.Comp);
        (int, bool) rollResult = (0, false);

        if (ent.Comp.Experience < ent.Comp.ExperienceCost || ent.Comp.Level >= 100)
            return false;

        int timesToRoll = ent.Comp.Experience / ent.Comp.ExperienceCost;
        ent.Comp.Experience -= ent.Comp.ExperienceCost * timesToRoll;
        (int, bool) rollInnard;
        for (int i = 0; i < timesToRoll; i++)
        {
            int diceType = DiceDictionary(ent);
            rollInnard = RollPenetrating(target, diceType);
            rollResult = (rollInnard.Item1, rollInnard.Item2 || rollResult.Item2);
            ent.Comp.Level += rollResult.Item1;
            if (rollInnard.Item2)
            {
                timesToRoll++;
            }
        }
        if (rollResult.Item2)
            _popup.PopupClient(Loc.GetString("knowledge-level-epiphany", ("knowledge", Name(ent))), target, target, PopupType.Medium);

        if (ent.Comp.Level > 100)
            ent.Comp.Level = 100;
        Dirty(ent);

        if (getMastery != GetMastery(ent.Comp) && !rollResult.Item2)
        {
            _popup.PopupClient(Loc.GetString("knowledge-level-up-popup", ("knowledge", Name(ent)), ("mastery", GetMasteryString(ent).ToLower())), target, target, PopupType.Medium);
        }

        return true;
    }

    private int DiceDictionary(Entity<KnowledgeComponent> ent)
    {
        return ent.Comp.Level switch
        {
            >= 88 => 3,
            >= 76 => 4,
            >= 51 => 6,
            >= 26 => 8,
            >= 1 => 12,
            _ => 20,
        };
    }

    public (ProtoId<KnowledgeCategoryPrototype> Category, KnowledgeInfo Info) GetKnowledgeInfo(Entity<KnowledgeComponent> ent)
    {
        var knowledgeInfo = new KnowledgeInfo("", "", ent.Comp.Color, ent.Comp.Sprite);
        // TODO: make this an event raised on ent
        var name = Name(ent);
        knowledgeInfo.Description = Loc.GetString("knowledge-info-description", ("level", ent.Comp.Level), ("mastery", GetMasteryString(ent)), ("exp", ent.Comp.Experience));
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
        else if (TryComp<MartialArtsKnowledgeComponent>(ent, out var martialKnowledge))
        {
            knowledgeInfo.Name = Loc.GetString("knowledge-martial-arts-name", ("name", name));
        }
        else
        {
            knowledgeInfo.Name = name;
        }
        return (ent.Comp.Category, knowledgeInfo);
    }

    /// <summary>
    /// Increase a knowledge unit's level for a target entity.
    /// This sets the level to max(current, new), NOT adding.
    /// If it does not exist it will be created.
    /// </summary>
    /// <returns>
    /// Null if spawning it fails.
    /// </returns>
    public Entity<KnowledgeComponent>? EnsureKnowledge(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id, int level = 0, bool popup = true)
    {
        if (GetKnowledge(ent, id) is {} existing)
        {
            if (existing.Comp.Level < level)
            {
                existing.Comp.Level = level;
                Dirty(existing, existing.Comp);
            }
            return existing;
        }

        PredictedTrySpawnInContainer(id, ent.Owner, KnowledgeContainerComponent.ContainerId, out var spawned);
        if (spawned is not {} unit)
        {
            Log.Error($"Failed to spawn knowledge {id} for {ToPrettyString(ent)}!");
            return null;
        }

        var comp = _query.Comp(unit);
        comp.Level = level;
        Dirty(unit, comp);

        ent.Comp.KnowledgeDict[id] = unit;
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.KnowledgeDict));

        if (ent.Comp.Holder is not {} holder)
            return (unit, comp); // added knowledge to a loose brain...

        var ev = new KnowledgeAddedEvent(ent, holder);
        RaiseLocalEvent(unit, ref ev);

        if (popup)
        {
            var msg = Loc.GetString("knowledge-unit-learned-popup", ("knowledge", Name(unit)));
            _popup.PopupPredicted(msg, holder, holder);
        }
        return (unit, comp);
    }

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public void AddKnowledgeUnits(EntityUid target, Dictionary<EntProtoId, int> knowledgeList, bool popup = true)
    {
        if (GetContainer(target) is not {} ent)
            return;

        foreach (var (id, level) in knowledgeList)
        {
            EnsureKnowledge(ent, id, level, popup);
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
        if (GetContainer(target) is not {} ent ||
            ent.Comp.Holder is not {} holder ||
            GetKnowledge(ent, id) is not {} unit ||
            unit.Comp.Unremoveable && !force)
            return null;

        ent.Comp.KnowledgeDict.Remove(id);
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.KnowledgeDict));

        var ev = new KnowledgeRemovedEvent(ent, holder);
        RaiseLocalEvent(ref ev);

        PredictedQueueDel(unit);

        _popup.PopupClient(Loc.GetString("knowledge-unit-forgotten-popup", ("knowledge", Name(unit))), holder, holder, PopupType.Medium);
        return target;
    }

    /// <summary>
    /// Gets a knowledge unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not a knowledge container, or if knowledge unit wasn't found.
    /// </returns>
    public override Entity<KnowledgeComponent>? GetKnowledge(EntityUid target, [ForbidLiteral] EntProtoId id)
        => GetContainer(target) is {} ent
            ? GetKnowledge(ent, id)
            : null;

    public Entity<KnowledgeComponent>? GetKnowledge(Entity<KnowledgeContainerComponent> ent, [ForbidLiteral] EntProtoId id)
        => ent.Comp.KnowledgeDict.TryGetValue(id, out var unit) && _query.TryComp(unit, out var comp)
            ? (unit, comp)
            : null;

    /// <summary>
    /// Returns all knowledge units inside the container component.
    /// </summary>
    public List<Entity<KnowledgeComponent>>? TryGetAllKnowledgeUnits(EntityUid target)
    {
        if (GetContainer(target) is not {} ent)
            return null;

        var found = new List<Entity<KnowledgeComponent>>();
        foreach (var unit in ent.Comp.KnowledgeDict.Values)
        {
            if (_query.TryComp(unit, out var comp))
                found.Add((unit, comp));
        }

        return found;
    }

    /// <summary>
    /// Returns the first knowledge entity of the target that has a given component.
    /// </summary>
    public EntityUid? HasKnowledgeComp<T>(EntityUid target) where T: IComponent
    {
        if (GetContainer(target)?.Comp.Container is not {} container)
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
    public List<Entity<T, KnowledgeComponent>>? GetKnowledgeWith<T>(EntityUid target) where T: IComponent
    {
        if (GetContainer(target)?.Comp.Container is not {} container)
            return null;

        var knowledgeEnts = new List<Entity<T, KnowledgeComponent>>();
        var query = GetEntityQuery<T>();
        foreach (var knowledge in container.ContainedEntities)
        {
            if (!_query.TryComp(knowledge, out var knowledgeComp))
                continue;

            if (query.TryComp(knowledge, out var comp))
                knowledgeEnts.Add((knowledge, comp, knowledgeComp));
        }

        return knowledgeEnts;
    }

    /// <summary>
    /// Returns true if that knowledge can be removed, by taking
    /// into account its memory level and knowledge category.
    /// </summary>
    public bool CanRemoveKnowledge(KnowledgeComponent comp, ProtoId<KnowledgeCategoryPrototype> category, int level)
        => !comp.Unremoveable && comp.Category == category && comp.Level <= level;

    public override void ClearKnowledge(EntityUid target, bool deleteAll)
    {
        if (GetContainer(target) is not {} ent)
            return;

        ent.Comp.KnowledgeDict.Clear();
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.KnowledgeDict));
        ChangeMartialArts(ent, target, null);
        ChangeLanguage(ent, null);
        if (deleteAll && ent.Comp.Container is {} container)
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
        if (_holderQuery.CompOrNull(uid)?.KnowledgeEntity is not {} ent)
            return null;

        return (ent, _containerQuery.Comp(ent));
    }

    /// <summary>
    /// Relays an event to all knowledge entities a mob has.
    /// </summary>
    public void RelayEvent<T>(Entity<KnowledgeHolderComponent> ent, ref T args) where T: notnull
    {
        if (GetContainer(ent)?.Comp.Container is not {} container)
            return;

        foreach (var unit in container.ContainedEntities)
        {
            RaiseLocalEvent(unit, ref args);
        }
    }

    public string GetMasteryString(Entity<KnowledgeComponent> ent)
        => ent.Comp.Level switch
        {
            >= 88 => Loc.GetString("knowledge-mastery-master"),
            >= 76 => Loc.GetString("knowledge-mastery-expert"),
            >= 51 => Loc.GetString("knowledge-mastery-advanced"),
            >= 26 => Loc.GetString("knowledge-mastery-average"),
            >= 1 => Loc.GetString("knowledge-mastery-novice"),
            _ => Loc.GetString("knowledge-mastery-unskilled"),
        };

    public override int GetMastery(int level)
        => level switch
        {
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
    /// </summary>
    public int GetLevel(EntityUid uid)
        => _query.CompOrNull(uid)?.Level ?? 0;

    public override int GetInverseMastery(int mastery)
        => mastery switch
        {
            >= 5 => 88,
            >= 4 => 76,
            >= 3 => 51,
            >= 2 => 26,
            >= 1 => 1,
            _ => 0,
        };

    public override float SharpCurve(Entity<KnowledgeComponent> knowledge, int offset = 0, float inverseScale = 100.0f)
        => SharpCurve(knowledge.Comp.Level, offset, inverseScale);

    public float SharpCurve(int level, int offset = 0, float inverseScale = 100f)
    {
        // ((level + offset)/inverseScale)^2
        // for level: [0, 100] and inverseScale = 100, this is just the graph of x^2 on [0, 1] :)
        var linear = (float) (level + offset) / inverseScale;
        return linear * linear;
    }

    public (int, bool) RollPenetrating(EntityUid uid, int sides, bool didCritical = false)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));
        var isCritical = false;
        int penetratingRolls = 0;
        int currentRoll = rand.Next(1, sides + 1);
        int total = currentRoll;
        int newSides = sides;

        while (currentRoll == newSides && penetratingRolls < 10)
        {
            penetratingRolls++;
            newSides = newSides switch
            {
                100 => 20,
                20 => 6,
                _ => newSides
            };
            currentRoll = rand.Next(1, newSides + 1);
            total += currentRoll - 1;
            isCritical = true;
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

    protected Entity<KnowledgeContainerComponent> EnsureKnowledgeContainer(Entity<KnowledgeHolderComponent> ent)
    {
        if (GetContainer(ent) is {} brain)
            return brain;

        // if there's no brain store knowledge on the mob itself
        var comp = EnsureComp<KnowledgeContainerComponent>(ent);
        LinkContainer(ent, (ent, comp));
        return (ent, comp);
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
