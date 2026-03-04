// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Construction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// This handles all knowledge related entities.
/// </summary>
public abstract partial class SharedKnowledgeSystem : CommonKnowledgeSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private TimeSpan _nextUpdate;
    private TimeSpan _updateDelay = TimeSpan.FromSeconds(1);
    private float _learnChance = 0.2f;
    private System.Random _seed = new System.Random(0);
    private EntityQuery<KnowledgeHolderComponent> _holderQuery;
    private EntityQuery<KnowledgeContainerComponent> _containerQuery;
    private EntityQuery<KnowledgeComponent> _knowledgeQuery;

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

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentShutdown>(OnKnowledgeContainerShutdown);
        SubscribeLocalEvent<KnowledgeContainerComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<MMIComponent, EntGotInsertedIntoContainerMessage>(OnMMIInserted);
        SubscribeLocalEvent<MMIComponent, EntGotRemovedFromContainerMessage>(OnMMIRemoved);

        //Experience Methods
        SubscribeLocalEvent<KnowledgeHolderComponent, AddExperienceEvent>(OnAddExperienceEvent);

        _holderQuery = GetEntityQuery<KnowledgeHolderComponent>();
        _containerQuery = GetEntityQuery<KnowledgeContainerComponent>();
        _knowledgeQuery = GetEntityQuery<KnowledgeComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + _updateDelay;

        var query = EntityQueryEnumerator<KnowledgeHolderComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (TryGetAllKnowledgeUnits(ent) is not { } knowledgeUnits)
                continue;

            foreach (var knowledgeUnit in knowledgeUnits)
            {
                if (RollForLevelUp(knowledgeUnit, (ent, comp)))
                    break;
            }
        }
    }


    private void OnKnowledgeContainerShutdown(Entity<KnowledgeContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.KnowledgeContainer is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnOrganInserted(Entity<KnowledgeContainerComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!TryComp<KnowledgeHolderComponent>(args.Target, out var knowledgeHolder))
            return;
        knowledgeHolder.KnowledgeEntity = ent;
        Dirty(args.Target, knowledgeHolder);
    }

    private void OnOrganRemoved(Entity<KnowledgeContainerComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TryComp<KnowledgeHolderComponent>(args.Target, out var knowledgeHolder))
            return;
        knowledgeHolder.KnowledgeEntity = null;
        Dirty(args.Target, knowledgeHolder);
    }

    private void OnMMIInserted(Entity<MMIComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!HasComp<BorgChassisComponent>(args.Container.Owner) || ent.Comp.BrainSlot.ContainerSlot?.ContainedEntity is not { } brain)
            return;
        var body = args.Container.Owner;
        if (!TryComp<KnowledgeHolderComponent>(body, out var knowledgeHolder))
            return;
        knowledgeHolder.KnowledgeEntity = brain;
        Dirty(body, knowledgeHolder);
    }

    private void OnMMIRemoved(Entity<MMIComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!HasComp<BorgChassisComponent>(args.Container.Owner) || ent.Comp.BrainSlot.ContainerSlot?.ContainedEntity is not { } brain)
            return;
        var body = args.Container.Owner;
        if (!TryComp<KnowledgeHolderComponent>(body, out var knowledgeHolder))
            return;
        if (knowledgeHolder.KnowledgeEntity == brain)
        {
            knowledgeHolder.KnowledgeEntity = null;
            Dirty(body, knowledgeHolder);
        }
    }

    public void OnAddExperienceEvent(Entity<KnowledgeHolderComponent> ent, ref AddExperienceEvent args)
    {
        if (TryGetKnowledgeUnit(ent, args.KnowledgeType) is not { } knowledgeUnit || !TryComp<KnowledgeComponent>(knowledgeUnit, out var knowledgeComponent))
        {
            _seed = new System.Random(SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id));
            if (_seed.Prob(_learnChance))
                TryAddKnowledgeUnit(ent, (args.KnowledgeType, 0));
            return;
        }
        ExperienceUpdate(knowledgeUnit, ent, ref args);

        var evNetUpdate = new UpdateExperienceEvent();
        RaiseLocalEvent(ent, ref evNetUpdate);
    }

    public void ExperienceUpdate(Entity<KnowledgeComponent> ent, Entity<KnowledgeHolderComponent> target, ref AddExperienceEvent args)
    {
        if (_timing.CurTime < ent.Comp.TimeToNextExperience)
            return;

        ent.Comp.TimeToNextExperience = _timing.CurTime + TimeSpan.FromSeconds(1);
        ent.Comp.Experience += args.Experience + ent.Comp.BonusExperience;

        RollForLevelUp(ent, target);
    }

    /// <summary>
    /// Rolls Levelup. True on roll. False on not.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool RollForLevelUp(Entity<KnowledgeComponent> ent, Entity<KnowledgeHolderComponent> target)
    {
        var getMastery = GetMastery(ent);
        (int, bool) rollResult = (0, false);

        if (!(ent.Comp.Experience >= ent.Comp.ExperienceCost && ent.Comp.Level < 100))
            return false;

        if (ent.Comp.OnSleep)
        {
            if (_mobState.IsCritical(target))
            {
                int diceType = DiceDictionary(ent);
                rollResult = RollPenetrating(diceType);
                if (!(rollResult.Item2))
                    return false;
                ent.Comp.Level += rollResult.Item1;
                _popup.PopupClient(Loc.GetString("knowledge-zenkai-boost"), target, target, PopupType.Large);
                _damageable.ClearAllDamage(target.Owner);
            }
            else if (HasComp<SleepingComponent>(target))
            {
                int diceType = DiceDictionary(ent);
                rollResult = RollPenetrating(diceType);
                if (!(rollResult.Item2))
                    return false;
            }
            else
                return false;
        }
        int timesToRoll = ent.Comp.Experience / ent.Comp.ExperienceCost;
        ent.Comp.Experience -= ent.Comp.ExperienceCost * timesToRoll;
        (int, bool) rollInnard;
        for (int i = 0; i < timesToRoll; i++)
        {
            int diceType = DiceDictionary(ent);
            rollInnard = RollPenetrating(diceType);
            rollResult = (rollInnard.Item1, rollInnard.Item2 || rollResult.Item2);
            ent.Comp.Level += rollResult.Item1;
            if (rollInnard.Item2)
            {
                timesToRoll++;
            }
        }
        if (rollResult.Item2)
            _popup.PopupClient(Loc.GetString("knowledge-level-epiphany", ("knowledge", KnowledgeString(ent))), target, target, PopupType.Medium);

        if (ent.Comp.Level > 100)
            ent.Comp.Level = 100;

        if (getMastery != GetMastery(ent) && !rollResult.Item2)
        {
            var knowledgePrototype = Prototype(ent)?.ID;
            _popup.PopupClient(Loc.GetString("knowledge-level-up-popup", ("knowledge", KnowledgeString(ent)), ("mastery", GetMasteryString(ent).ToLower())), target, target, PopupType.Medium);
        }

        Dirty(ent);
        Dirty(target);
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

    public override (string Category, KnowledgeInfo Info) GetKnowledgeInfo(Entity<KnowledgeComponent> ent)
    {
        var knowledgeInfo = new KnowledgeInfo("", "", ent.Comp.Color, ent.Comp.Sprite);
        var knowledgePrototype = Prototype(ent)?.ID;
        knowledgeInfo.Description = Loc.GetString("knowledge-info-description", ("level", ent.Comp.Level), ("mastery", GetMasteryString(ent)), ("exp", ent.Comp.Experience));
        if (TryComp<LanguageKnowledgeComponent>(ent, out var languageKnowledge))
        {
            var langName = _language.GetLanguagePrototype(languageKnowledge.LanguageId)?.Name ?? Loc.GetString("generic-error");

            var locKey = (languageKnowledge.Speaks, languageKnowledge.Understands) switch
            {
                (true, true) => "knowledge-language-speaks-understands",
                (true, false) => "knowledge-language-speaks",
                _ => "knowledge-language-understands"
            };

            knowledgeInfo.Name = Loc.GetString(locKey, ("language", langName));
        }
        else if (TryComp<MartialArtsKnowledgeComponent>(ent, out var martialKnowledge))
        {
            knowledgeInfo.Name = Loc.GetString("knowledge-martial-arts-name", ("name", Loc.GetString($"knowledge-{knowledgePrototype}")));
        }
        else
        {
            knowledgeInfo.Name = Loc.GetString($"knowledge-{knowledgePrototype}");
        }
        return (ent.Comp.Category, knowledgeInfo);
    }

    /// <summary>
    /// Ensures that knowledge unit exists inside an entity, and adds it if it's not already here.
    /// </summary>
    /// <returns>
    /// False if or failed to spawn a knowledge unit inside it, true if unit was found or spawned successfully.
    /// </returns>
    public override bool TryEnsureKnowledgeUnit(
        EntityUid target,
        EntProtoId knowledgeId,
        [NotNullWhen(true)] out EntityUid? found)
    {
        found = null;
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent))
            return false;

        var ent = EnsureKnowledgeContainer((target, holderComponent));
        EnsureContainer(ent);

        if (TryGetKnowledgeUnit(ent.Owner, knowledgeId) is { } uid)
        {
            found = uid;
            return true;
        }

        return PredictedTrySpawnInContainer(knowledgeId, ent.Owner, KnowledgeContainerComponent.ContainerId, out found);
    }

    /// <summary>
    /// Returns the knowledge unit.
    /// </summary>
    /// <returns>
    /// Null if no unit found.
    /// </returns>
    public override Entity<KnowledgeComponent>? TryAddKnowledgeUnit(EntityUid target, (EntProtoId, int) knowledgeId)
    {
        Entity<KnowledgeComponent>? knowledgeEnt = null;

        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent))
            return knowledgeEnt;

        if (TryGetKnowledgeContainer((target, holderComponent)) is not { } entVerified)
            return null;

        Container? container = null;

        container = EnsureContainer(entVerified);

        if (TryGetKnowledgeUnit(target, knowledgeId.Item1) is { } uid)
        {
            if (TryComp<KnowledgeComponent>(uid, out var knowledgeComp) && knowledgeComp.Level < knowledgeId.Item2)
            {
                knowledgeComp.Level = knowledgeId.Item2;
                Dirty(uid, knowledgeComp);
                knowledgeEnt = (uid, knowledgeComp);
            }
        }
        else
        {
            var result = PredictedTrySpawnInContainer(knowledgeId.Item1, entVerified.Owner, KnowledgeContainerComponent.ContainerId, out var knowledgeUnit);
            if (!result || knowledgeUnit is not { } knowledgeUnitVerified)
                return knowledgeEnt;
            if (TryComp<KnowledgeComponent>(knowledgeUnitVerified, out var knowledgeComp))
            {
                knowledgeComp.Level = knowledgeId.Item2;
                knowledgeEnt = (knowledgeUnitVerified, knowledgeComp);
                Dirty(knowledgeUnitVerified, knowledgeComp);
            }
            entVerified.Comp.KnowledgeContainerIDs[knowledgeId.Item1] = knowledgeUnitVerified;

            if (TryComp<LanguageKnowledgeComponent>(knowledgeUnitVerified, out var languageComp))
                EnsureComp<LanguageSpeakerComponent>(target);

            _popup.PopupClient(Loc.GetString("knowledge-unit-learned-popup", ("knowledge", KnowledgeString(knowledgeUnitVerified))), target, target, PopupType.Medium);
        }
        Dirty(entVerified);
        return knowledgeEnt;
    }

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public override void AddKnowledgeUnits(EntityUid target, Dictionary<EntProtoId, int> knowledgeList)
    {
        var comp = EnsureComp<KnowledgeHolderComponent>(target);

        foreach (var knowledgeId in knowledgeList)
        {
            TryAddKnowledgeUnit(target, (knowledgeId.Key, knowledgeId.Value));
        }

        var evNetUpdate = new UpdateExperienceEvent();
        RaiseLocalEvent(target, ref evNetUpdate);
    }

    /// <summary>
    /// Removes a knowledge unit from a container. Will not remove a knowledge unit if it's marked as unremoveable,
    /// unless force parameter is true.
    /// </summary>
    public override EntityUid? TryRemoveKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit, bool force = false)
    {
        if (TryGetKnowledgeUnit(target, knowledgeUnit) is not { } unit || !TryComp<KnowledgeComponent>(unit, out var knowledge))
            return null;

        if (!force && knowledge.Unremoveable)
            return null;

        if (TryComp<KnowledgeHolderComponent>(target, out var holderComponent) && TryGetKnowledgeEntity((target, holderComponent)) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainer))
        {
            if (knowledgeContainer.MartialArtSkillUid == unit)
                ChangeMartialArts(target, null);
            if (knowledgeContainer.LanguageSkillUid == unit)
                knowledgeContainer.LanguageSkillUid = null;
            knowledgeContainer.KnowledgeContainerIDs.Remove(knowledgeUnit);
        }

        PredictedQueueDel(unit);
        _popup.PopupClient(Loc.GetString("knowledge-unit-forgotten-popup", ("knowledge", KnowledgeString(unit))), target, target, PopupType.Medium);
        return target;
    }

    /// <summary>
    /// Same as TryRemoveKnowledgeUnit, but instead of removing one specific units, runs it on all knowledge units at once.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container.
    /// </returns>
    public override EntityUid? TryRemoveAllKnowledgeUnits(EntityUid target, ProtoId<KnowledgeCategoryPrototype> category, int level, bool force = false)
    {
        if (TryGetAllKnowledgeUnits(target) is not { } units)
            return null;

        foreach (var unit in units)
        {
            if (CanRemoveKnowledge(unit.AsNullable(), category, level, force) is not { })
                continue;

            PredictedQueueDel(unit.Owner);
        }

        return target;
    }

    /// <summary>
    /// Same as TryRemoveKnowledgeUnit, but instead of removing one specific units, runs it on all knowledge units at once.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container.
    /// </returns>
    public override EntityUid? TryRemoveAllKnowledgeUnits(EntityUid target, bool force = false)
    {
        if (TryGetAllKnowledgeUnits(target) is not { } units)
            return null;

        foreach (var (unit, knowledgeComp) in units)
        {
            if (!force && knowledgeComp.Unremoveable)
                continue;

            PredictedQueueDel(unit);
        }

        return target;
    }

    /// <summary>
    /// Gets a knowledge unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not a knowledge container, or if knowledge unit wasn't found.
    /// </returns>
    public override Entity<KnowledgeComponent>? TryGetKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit)
    {
        if (!_holderQuery.TryComp(target, out var holderComponent) || TryGetKnowledgeContainer((target, holderComponent)) is not { } ent)
            return null;

        if (ent.Comp.KnowledgeContainerIDs.TryGetValue(knowledgeUnit, out var knowledge) && _knowledgeQuery.TryComp(knowledge, out var knowledgeComponent))
            return (knowledge, knowledgeComponent);
        else
            return null;
    }

    /// <summary>
    /// Returns all knowledge units inside the container component.
    /// </summary>
    public override List<Entity<KnowledgeComponent>>? TryGetAllKnowledgeUnits(EntityUid target)
    {
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent) || TryGetKnowledgeEntity((target, holderComponent)) is not { } ent || !TryComp<KnowledgeContainerComponent>(ent, out var comp))
            return null;

        var found = new List<Entity<KnowledgeComponent>>();

        foreach (var knowledge in comp.KnowledgeContainerIDs)
        {
            if (TryComp<KnowledgeComponent>(knowledge.Value, out var knowledgeComponent))
                found.Add((knowledge.Value, knowledgeComponent));
        }

        return found;
    }

    /// <summary>
    /// Checks if the specified component is present on any of the entity's knowledge.
    /// </summary>
    public override EntityUid? HasKnowledgeComp<T>(EntityUid target)
    {
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent))
            return null;

        if (TryGetKnowledgeContainer((target, holderComponent)) is not { } entVerified)
            return null;

        Container? container = null;

        container = EnsureContainer(entVerified);

        foreach (var knowledge in container.ContainedEntities)
        {
            if (HasComp<T>(knowledge))
                return target;
        }

        return null;
    }

    /// <summary>
    /// Returns all knowledge that have the specified component.
    /// </summary>
    public override List<Entity<T, KnowledgeComponent>>? TryGetKnowledgeWithComp<T>(EntityUid target)
    {
        List<Entity<T, KnowledgeComponent>>? knowledgeEnts = null;
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent))
            return knowledgeEnts;

        if (TryGetKnowledgeContainer((target, holderComponent)) is not { } entVerified)
            return null;

        Container? container = null;

        container = EnsureContainer(entVerified);

        foreach (var knowledge in container.ContainedEntities)
        {
            if (!TryComp<KnowledgeComponent>(knowledge, out var knowledgeComp))
                continue;

            if (TryComp<T>(knowledge, out var comp))
            {
                knowledgeEnts ??= [];
                knowledgeEnts.Add((knowledge, comp, knowledgeComp));
            }
        }

        return knowledgeEnts;
    }

    /// <summary>
    /// Returns true if that knowledge can be removed, by taking
    /// into account its memory level and knowledge category.
    /// </summary>
    public override EntityUid? CanRemoveKnowledge(Entity<KnowledgeComponent?> target, ProtoId<KnowledgeCategoryPrototype> category, int level, bool force = false)
    {
        if (!TryComp<KnowledgeComponent>(target, out var component))
            return null;

        if (force)
            return target;

        if (component.Unremoveable || component.Category != category || component.Level > level)
            return null;

        return target;
    }

    public override Entity<KnowledgeContainerComponent>? TryGetKnowledgeContainer(Entity<KnowledgeHolderComponent> ent)
    {
        if (ent.Comp.KnowledgeEntity is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainer))
            return (knowledgeEnt, knowledgeContainer);
        return null;
    }

    public override Entity<KnowledgeContainerComponent> EnsureKnowledgeContainer(Entity<KnowledgeHolderComponent> ent)
    {
        if (TryGetKnowledgeContainer(ent) is { } knowledgeContainer)
            return knowledgeContainer;

        // If not found just give up
        var knowledge = EnsureComp<KnowledgeContainerComponent>(ent);
        ent.Comp.KnowledgeEntity = ent;
        Dirty(ent.Owner, ent.Comp);
        return (ent, knowledge);
    }

    public override EntityUid? TryGetKnowledgeEntity(EntityUid uid)
    {
        if (!TryComp<KnowledgeHolderComponent>(uid, out var knowledgeHolder))
            return null;

        if (knowledgeHolder.KnowledgeEntity is { })
            return knowledgeHolder.KnowledgeEntity;

        return TryGetKnowledgeContainer((uid, knowledgeHolder));
    }

    public override EntityUid? TryGetKnowledgeEntity(Entity<KnowledgeHolderComponent> ent)
    {
        if (ent.Comp.KnowledgeEntity is { })
            return ent.Comp.KnowledgeEntity;

        return TryGetKnowledgeContainer(ent);
    }


    public override void ClearKnowledge(EntityUid target, bool deleteAll)
    {
        if (!TryComp<KnowledgeHolderComponent>(target, out var holder) || TryGetKnowledgeContainer((target, holder)) is not { } knowledgeContainer)
            return;

        knowledgeContainer.Comp.KnowledgeContainerIDs.Clear();
        ChangeMartialArts(target, null);
        knowledgeContainer.Comp.LanguageSkillUid = null;
        var container = knowledgeContainer.Comp.KnowledgeContainer;
        if (container is { } && deleteAll)
        {
            foreach (var entity in container.ContainedEntities)
            {
                PredictedQueueDel(entity);
            }
        }
    }

    public override Dictionary<EntProtoId, EntityUid>? TryGetKnowledgeDictionary(EntityUid target)
    {
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent) || TryGetKnowledgeEntity((target, holderComponent)) is not { } ent || !TryComp<KnowledgeContainerComponent>(ent, out var comp))
            return null;
        return comp.KnowledgeContainerIDs;
    }

    public string GetMasteryString(Entity<KnowledgeComponent> ent)
    {
        return ent.Comp.Level switch
        {
            >= 88 => Loc.GetString("knowledge-mastery-master"),
            >= 76 => Loc.GetString("knowledge-mastery-expert"),
            >= 51 => Loc.GetString("knowledge-mastery-advanced"),
            >= 26 => Loc.GetString("knowledge-mastery-average"),
            >= 1 => Loc.GetString("knowledge-mastery-novice"),
            _ => Loc.GetString("knowledge-mastery-unskilled"),
        };
    }

    public override int GetMastery(Entity<KnowledgeComponent> ent)
    {
        return ent.Comp.Level switch
        {
            >= 88 => 5,
            >= 76 => 4,
            >= 51 => 3,
            >= 26 => 2,
            >= 1 => 1,
            _ => 0,
        };
    }

    public override int GetMastery(EntityUid uid)
    {
        if (TryComp<KnowledgeComponent>(uid, out var comp))
            return GetMastery((uid, comp));
        else
            return 0;
    }

    public override int GetInverseMastery(int number)
    {
        return number switch
        {
            >= 5 => 88,
            >= 4 => 76,
            >= 3 => 51,
            >= 2 => 26,
            >= 1 => 1,
            _ => 0,
        };
    }

    public override float SharpCurve(Entity<KnowledgeComponent> knowledge, int offset = 0, float inverseScale = 100.0f)
    {
        return ((float) (knowledge.Comp.Level + offset) / inverseScale) * ((float) (knowledge.Comp.Level + offset) / inverseScale);
    }

    public override float InverseSharpCurve(Entity<KnowledgeComponent> knowledge, int offset = 0, float inverseScale = 100.0f)
    {
        return ((float) (offset - knowledge.Comp.Level) / inverseScale) * ((float) (offset - knowledge.Comp.Level) / inverseScale);
    }

    public (int, bool) RollPenetrating(int sides, bool didCritical = false)
    {

        bool isCritical = false;
        int penetratingRolls = 0;
        int currentRoll = _seed.Next(1, sides + 1);
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
            currentRoll = _seed.Next(1, newSides + 1);
            total += currentRoll - 1;
            isCritical = true;
        }

        return (total, isCritical);
    }

    private Container EnsureContainer(Entity<KnowledgeContainerComponent> ent)
    {
        if (ent.Comp.KnowledgeContainer != null)
            return ent.Comp.KnowledgeContainer;

        ent.Comp.KnowledgeContainer = _container.EnsureContainer<Container>(ent.Owner, KnowledgeContainerComponent.ContainerId);
        // We show the contents of the container to allow knowledge to have visible sprites. I mean, if you really need to show some big brains.
        ent.Comp.KnowledgeContainer.ShowContents = true;

        return ent.Comp.KnowledgeContainer;
    }

    public string KnowledgeString(EntityUid knowledgeUnit)
    {
        return Loc.GetString($"knowledge-{Prototype(knowledgeUnit)?.ID}");
    }
}
