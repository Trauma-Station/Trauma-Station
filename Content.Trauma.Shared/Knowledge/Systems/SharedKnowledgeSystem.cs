using System.Diagnostics.CodeAnalysis;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Construction;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
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
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private EntityQuery<KnowledgeComponent> _knowledgeQuery;
    private EntityQuery<KnowledgeContainerComponent> _containerQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeLanguage();
        InitializeMartialArts();
        InitializeOnWear();

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentShutdown>(OnKnowledgeContainerShutdown);
        SubscribeLocalEvent<KnowledgeContainerComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<KnowledgeHolderComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<KnowledgeHolderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<KnowledgeContainerComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEvent);
        SubscribeLocalEvent<BodyComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEventBodyPart);


        //Experience Methods
        SubscribeLocalEvent<KnowledgeHolderComponent, AddExperience>(OnAddExperience);

        _knowledgeQuery = GetEntityQuery<KnowledgeComponent>();
        _containerQuery = GetEntityQuery<KnowledgeContainerComponent>();
    }

    private void OnKnowledgeContainerShutdown(Entity<KnowledgeContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.KnowledgeContainer is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnEntityInserted(Entity<KnowledgeContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != KnowledgeContainerComponent.ContainerId
            || !_knowledgeQuery.TryComp(args.Entity, out var statusComp))
            return;

        // Make sure AppliedTo is set correctly so events can rely on it
        if (statusComp.AppliedTo != ent)
        {
            statusComp.AppliedTo = ent;
            Dirty(args.Entity, statusComp);
        }

        var ev = new KnowledgeUnitAddedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);
    }

    private void OnEntityRemoved(Entity<KnowledgeContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != KnowledgeContainerComponent.ContainerId
            || !_knowledgeQuery.TryComp(args.Entity, out var statusComp))
            return;

        var ev = new KnowledgeUnitRemovedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);

        // Clear AppliedTo after events are handled so event handlers can use it.
        if (statusComp.AppliedTo == null)
            return;

        // Why not just delete it? Well, that might end up being best, but this
        // could theoretically allow for moving status effects from one entity
        // to another. That might be good to have for polymorphs or something.
        statusComp.AppliedTo = null;
        Dirty(args.Entity, statusComp);
    }

    private void OnEntInserted(Entity<KnowledgeHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryFindKnowledgeInEntity(ent, out var brain))
            return;

        ent.Comp.KnowledgeEntity = brain;
        Dirty(ent);
    }

    private void OnEntRemoved(Entity<KnowledgeHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (ent.Comp.KnowledgeEntity == null)
            return;

        var brain = ent.Comp.KnowledgeEntity.Value;

        if (args.Entity == brain || !IsDescendantOf(ent, brain))
        {
            ent.Comp.KnowledgeEntity = null;
            Dirty(ent);
        }
    }

    private bool TryFindKnowledgeInEntity(EntityUid parent, out EntityUid brain)
    {
        brain = default;

        if (HasComp<KnowledgeContainerComponent>(parent))
        {
            brain = parent;
            return true;
        }

        var xform = Transform(parent);
        var enumerator = xform.ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            if (TryFindKnowledgeInEntity(child, out brain))
                return true;
        }

        return false;
    }

    private bool IsDescendantOf(EntityUid potentialParent, EntityUid child)
    {
        if (!potentialParent.IsValid() || !child.IsValid())
            return false;

        var current = child;

        while (current.IsValid() && TryComp<TransformComponent>(current, out var xform))
        {
            if (xform.ParentUid == potentialParent)
                return true;

            current = xform.ParentUid;

            if (HasComp<MapComponent>(current) || HasComp<MapGridComponent>(current))
                break;
        }

        return false;
    }

    private void OnHolderStartup(Entity<KnowledgeHolderComponent> ent, ref ComponentStartup args)
    {
        SetupHolder(ent);
    }

    private void OnContainerStartup(Entity<KnowledgeContainerComponent> ent, ref ComponentStartup args)
    {
        FindEntityHolder(ent);
    }

    public void SetupHolder(Entity<KnowledgeHolderComponent> ent)
    {
        var ev = new KnowledgeContainerRelayEvent(ent);
        RecursiveRaiseRelayEvent(ent, ref ev);

        // Check entity that we have found
        if (_containerQuery.TryComp(ev.Found, out var knowledgeFound))
            ent.Comp.KnowledgeEntity = ev.Found;
        Dirty(ent.Owner, ent.Comp);
        if (TryComp<LanguageSpeakerComponent>(ent.Owner, out var languageSpeaker))
            UpdateEntityLanguages((ent, languageSpeaker));
    }

    public void FindEntityHolder(Entity<KnowledgeContainerComponent> ent)
    {
        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        var bodyUid = container.Owner;

        if (!HasComp<KnowledgeHolderComponent>(bodyUid))
        {
            AddComp<KnowledgeHolderComponent>(bodyUid);
        }
        if (TryComp<KnowledgeHolderComponent>(bodyUid, out var holder))
            SetupHolder((bodyUid, holder));
    }

    //public void OnInitContainer(Entity<KnowledgeContainerComponent> ent, ref MapInitEvent args)
    //{
    //    Log.Debug($"Initializing Knowledge Container for {ToPrettyString(ent.Owner)}");
    //    if (!TryComp<OrganComponent>(ent, out var organComponent))
    //        return;
    //    Log.Debug($"Found Organ Component for {ToPrettyString(ent.Owner)}");
    //    if (organComponent.Body is not { } ownerUid)
    //        return;
    //    Log.Debug($"Initializing Knowledge Container for {ToPrettyString(ownerUid)}");
    //    EnsureComp<KnowledgeHolderComponent>(ownerUid, out var knowledgeHolder);
    //    knowledgeHolder.KnowledgeEntity = ent.Owner;
    //}

    public void OnInit(Entity<KnowledgeHolderComponent> ent)
    {
        EnsureKnowledgeContainer(ent.Owner, out var knowledgeContainer);
        ent.Comp.KnowledgeEntity = knowledgeContainer.Owner;
        Dirty(ent.Owner, ent.Comp);

        if (TryComp<LanguageSpeakerComponent>(ent.Owner, out var languageSpeaker))
            UpdateEntityLanguages((ent, languageSpeaker));

    }

    public void OnAddExperience(Entity<KnowledgeHolderComponent> ent, ref AddExperience args)
    {
        if (TryGetKnowledgeEntity(ent.Owner) is not { } knowledgeEntity || !TryComp<KnowledgeContainerComponent>(knowledgeEntity, out var knowledgeContainer))
            return;
        if (TryGetKnowledgeUnit(ent, args.KnowledgeType) is not { } knowledgeUnit || !TryComp<KnowledgeComponent>(knowledgeUnit, out var knowledgeComponent))
        {
            if (_random.Prob(0.2f))
                TryAddKnowledgeUnit(ent, new KeyValuePair<EntProtoId, int>(args.KnowledgeType, 0));
            return;
        }
        var knowledge = (knowledgeUnit, knowledgeComponent);

        var getMastery = GetMastery(knowledge);
        knowledgeComponent.Experience += args.Experience + knowledgeComponent.BonusExperience;
        if (knowledgeComponent.Experience >= knowledgeComponent.ExperienceCost || knowledgeComponent.Level < 100)
        {
            _random.SetSeed((int) _timing.CurTick.Value);
            int timesToRoll = knowledgeComponent.Experience / knowledgeComponent.ExperienceCost;
            for (int i = 0; i < timesToRoll; i++)
            {
                knowledgeComponent.Experience -= knowledgeComponent.ExperienceCost;
                int diceType = knowledgeComponent.Level switch
                {
                    >= 88 => 3,
                    >= 76 => 4,
                    >= 51 => 6,
                    >= 26 => 8,
                    >= 1 => 12,
                    _ => 20,
                };
                var rollResult = RollPenetrating(diceType);
                knowledgeComponent.Level += rollResult.Item1;
                var knowledgePrototype = MetaData(knowledgeUnit).EntityPrototype?.ID;
                if (rollResult.Item2)
                {
                    if (TryComp<LanguageKnowledgeComponent>(knowledgeUnit, out var knowledgeComp))
                        _popup.PopupEntity(Loc.GetString("knowledge-level-epiphany", ("knowledge", Loc.GetString($"{knowledgeComp.LanguageId.Id}"))), ent, ent, PopupType.Medium);
                    else
                        _popup.PopupEntity(Loc.GetString("knowledge-level-epiphany", ("knowledge", Loc.GetString($"knowledge-{knowledgePrototype}"))), ent, ent, PopupType.Medium);
                }
            }
        }
        if (knowledgeComponent.Level > 100)
            knowledgeComponent.Level = 100;
        if (getMastery != GetMastery(knowledge))
        {
            var knowledgePrototype = MetaData(knowledgeUnit).EntityPrototype?.ID;
            if (TryComp<LanguageKnowledgeComponent>(knowledgeUnit, out var knowledgeComp))
                _popup.PopupEntity(Loc.GetString("knowledge-level-up-popup", ("knowledge", Loc.GetString($"{knowledgeComp.LanguageId.Id}")), ("mastery", GetCurrentMastery(knowledge).ToLower())), ent, ent, PopupType.Medium);
            else
                _popup.PopupEntity(Loc.GetString("knowledge-level-up-popup", ("knowledge", Loc.GetString($"knowledge-{knowledgePrototype}")), ("mastery", GetCurrentMastery(knowledge).ToLower())), ent, ent, PopupType.Medium);
        }
        Dirty(ent);
    }

    public override (string Category, KnowledgeInfo Info) GetKnowledgeInfo(Entity<KnowledgeComponent> ent)
    {
        var category = _protoMan.Index(ent.Comp.Category);

        var knowledgeInfo = new KnowledgeInfo("", "", ent.Comp.Color, ent.Comp.Sprite);
        var knowledgePrototype = MetaData(ent).EntityPrototype?.ID;
        knowledgeInfo.Description = Loc.GetString("knowledge-info-description", ("level", ent.Comp.Level), ("mastery", GetCurrentMastery(ent)), ("exp", ent.Comp.Experience));
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
        else if (TryComp<ConstructionKnowledgeComponent>(ent, out var constructionKnowledge))
        {
            knowledgeInfo.Name = Loc.GetString("knowledge-construction-name", ("group", Loc.GetString($"knowledge-{knowledgePrototype}")));
        }
        else if (TryComp<MartialArtsKnowledgeComponent>(ent, out var martialKnowledge))
        {
            knowledgeInfo.Name = Loc.GetString("knowledge-martial-arts-name", ("name", Loc.GetString($"knowledge-{knowledgePrototype}")));
        }
        else
        {
            knowledgeInfo.Name = Loc.GetString($"knowledge-{knowledgePrototype}");
        }
        return (Loc.GetString(category.Name), knowledgeInfo);
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
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent);

        if (TryGetKnowledgeUnit(ent.Owner, knowledgeId) is { } uid)
        {
            found = uid;
            return true;
        }

        return PredictedTrySpawnInContainer(knowledgeId, ent.Owner, KnowledgeContainerComponent.ContainerId, out found);
    }

    /// <summary>
    /// Adds a knowledge unit to a knowledge container.
    /// </summary>
    /// <returns>
    /// False if container already has knowledge entity with that ID.
    /// </returns>
    public override bool TryAddKnowledgeUnit(EntityUid target, KeyValuePair<EntProtoId, int> knowledgeId)
    {
        return TryAddKnowledgeUnit(target, knowledgeId, out _);
    }

    /// <inheretdoc cref="TryAddKnowledgeUnit(Robust.Shared.GameObjects.EntityUid, System.Collections.Generic.KeyValuePair{Content.Shared.EntityTable.EntitySelectors.EntProtoId, int})"/>
    public override bool TryAddKnowledgeUnit(EntityUid target, KeyValuePair<EntProtoId, int> knowledgeId, [NotNullWhen(true)] out EntityUid? knowledgeUnit)
    {
        knowledgeUnit = null;

        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent);

        if (TryGetKnowledgeUnit(ent.Owner, knowledgeId.Key) is { } uid)
        {
            knowledgeUnit = uid;
            if (TryComp<KnowledgeComponent>(uid, out var knowledgeComp) && knowledgeComp.Level < knowledgeId.Value)
            {
                knowledgeComp.Level = knowledgeId.Value;
            }
            Dirty(ent);
            return false;
        }
        else
        {
            if (_netManager.IsClient)
                return false;

            var result = PredictedTrySpawnInContainer(knowledgeId.Key, ent.Owner, KnowledgeContainerComponent.ContainerId, out knowledgeUnit);
            if (!result || knowledgeUnit is not { } knowledgeUnitVerified)
                return false;
            if (TryComp<KnowledgeComponent>(knowledgeUnitVerified, out var knowledgeComp))
            {
                knowledgeComp.Level = knowledgeId.Value;
                Dirty(knowledgeUnitVerified, knowledgeComp);
            }
            ent.Comp.KnowledgeContainerIDs[knowledgeId.Key] = knowledgeUnitVerified;
            if (TryComp<LanguageKnowledgeComponent>(knowledgeUnitVerified, out var languageComp))
            {
                EnsureComp<LanguageSpeakerComponent>(target);
                _popup.PopupEntity(Loc.GetString("knowledge-unit-learned-popup", ("knowledge", Loc.GetString($"{languageComp.LanguageId.Id}"))), target, target, PopupType.Medium);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("knowledge-unit-learned-popup", ("knowledge", Loc.GetString($"knowledge-{knowledgeId.Key.ToString()}"))), target, target, PopupType.Medium);

            }
            Dirty(ent);
            return true;
        }
    }

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public override void AddKnowledgeUnits(EntityUid target, Dictionary<EntProtoId, int> knowledgeList)
    {
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent);

        // Log.Debug($"Adding {knowledgeList.Count()} knowledge units to {ToPrettyString(target)}");

        foreach (var knowledgeId in knowledgeList)
        {
            TryAddKnowledgeUnit(target, knowledgeId);
        }
        var comp = EnsureComp<KnowledgeHolderComponent>(target);
        OnInit((target, comp));
    }

    /// <summary>
    /// Removes a knowledge unit from a container. Will not remove a knowledge unit if it's marked as unremoveable,
    /// unless force parameter is true.
    /// </summary>
    public override EntityUid? TryRemoveKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit, bool force = false)
    {
        if (TryGetKnowledgeUnit(target, knowledgeUnit) is not { } unit || !_knowledgeQuery.TryComp(unit, out var knowledge))
            return null;

        if (!force && knowledge.Unremoveable)
            return null;

        if (TryGetKnowledgeEntity(target) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainer))
        {
            if (knowledgeContainer.MartialArtSkillUid == unit)
                knowledgeContainer.MartialArtSkillUid = null;
            if (knowledgeContainer.LanguageSkillUid == unit)
                knowledgeContainer.LanguageSkillUid = null;
            knowledgeContainer.KnowledgeContainerIDs.Remove(knowledgeUnit);
        }

        PredictedQueueDel(unit);
        if (TryComp<LanguageKnowledgeComponent>(unit, out _))
        {
            _popup.PopupEntity(Loc.GetString("knowledge-unit-forgotten-popup", ("knowledge", Loc.GetString($"{knowledgeUnit.ToString()}"))), target, target, PopupType.Medium);
        }
        else
            _popup.PopupEntity(Loc.GetString("knowledge-unit-forgotten-popup", ("knowledge", Loc.GetString($"knowledge-{knowledgeUnit.ToString()}"))), target, target, PopupType.Medium);
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
    public override EntityUid? TryGetKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit)
    {
        if (TryGetKnowledgeEntity(target) is not { } ent || !TryComp<KnowledgeContainerComponent>(ent, out var comp))
            return null;

        if (comp.KnowledgeContainerIDs.TryGetValue(knowledgeUnit, out var knowledge))
            return knowledge;
        else
            return null;
    }

    /// <summary>
    /// Returns all knowledge units inside the container component.
    /// </summary>
    public override List<Entity<KnowledgeComponent>>? TryGetAllKnowledgeUnits(EntityUid target)
    {
        List<Entity<KnowledgeComponent>>? found = null;
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent, out var container);

        foreach (var unit in container.ContainedEntities)
        {
            if (!_knowledgeQuery.TryComp(unit, out var knowledgeComp))
                continue;

            found ??= [];
            found.Add((unit, knowledgeComp));
        }

        return found;
    }

    /// <summary>
    /// Checks if the specified component is present on any of the entity's knowledge.
    /// </summary>
    public override EntityUid? HasKnowledgeComp<T>(EntityUid target)
    {
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent, out var container);

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
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent, out var container);

        foreach (var knowledge in container.ContainedEntities)
        {
            if (!_knowledgeQuery.TryComp(knowledge, out var knowledgeComp))
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
        if (!_knowledgeQuery.Resolve(target.Owner, ref target.Comp))
            return null;

        if (force)
            return target;

        if (target.Comp.Unremoveable
            || target.Comp.Category != category
            || target.Comp.Level > level)
            return null;

        return target;
    }

    /// <summary>
    /// Gets a knowledge container from an entity.
    /// Since sometimes the entity itself is a knowledge container, and sometimes it's contained in the brain,
    /// we have to sometimes relay to the brain entity to get knowledge properly.
    /// </summary>
    /// <param name="uid">Main entity from which we are trying to get</param>
    /// <returns>Entity that contains knowledge related to original uid.</returns>
    public override Entity<KnowledgeContainerComponent> EnsureKnowledgeContainer(EntityUid uid)
    {
        // Raise event on all children
        var ev = new KnowledgeContainerRelayEvent(uid);
        RecursiveRaiseRelayEvent(uid, ref ev);

        // Check entity that we have found
        if (_containerQuery.TryComp(ev.Found, out var knowledgeFound))
            return (ev.Found.Value, knowledgeFound);

        // If not found just five up
        var knowledge = EnsureComp<KnowledgeContainerComponent>(uid);
        return (uid, knowledge);
    }

    /// <inheritdoc cref="EnsureKnowledgeContainer(Robust.Shared.GameObjects.EntityUid)"/>
    public override void EnsureKnowledgeContainer(EntityUid uid, out Entity<KnowledgeContainerComponent> container)
    {
        // Raise event on all children
        var ev = new KnowledgeContainerRelayEvent(uid);
        RecursiveRaiseRelayEvent(uid, ref ev);

        // Check entity that we have found
        if (_containerQuery.TryComp(ev.Found, out var knowledgeFound))
        {
            container = (ev.Found.Value, knowledgeFound);
            return;
        }

        // If not found just give up and ensure it on the entity itself
        var knowledge = EnsureComp<KnowledgeContainerComponent>(uid);
        container = (uid, knowledge);
    }

    public override EntityUid? TryGetKnowledgeEntity(EntityUid ent)
    {
        if (TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder) && knowledgeHolder.KnowledgeEntity is { })
            return knowledgeHolder.KnowledgeEntity;
        // Raise event on all children
        var ev = new KnowledgeContainerRelayEvent(ent);
        RecursiveRaiseRelayEvent(ent, ref ev);

        // Check entity that we have found
        if (_containerQuery.TryComp(ev.Found, out var knowledgeFound))
            return ev.Found;

        return null;
    }

    public override void ChangeMartialArts(EntityUid knowledgeEntity, Entity<MartialArtsKnowledgeComponent>? martialArt)
    {
        if (!TryComp<KnowledgeContainerComponent>(knowledgeEntity, out var knowledgeContainer))
            return;

        knowledgeContainer.MartialArtSkillUid = martialArt;
    }

    public override void ClearKnowledge(EntityUid target, bool deleteAll)
    {
        if (TryComp<KnowledgeContainerComponent>(target, out var knowledgeContainer))
        {
            knowledgeContainer.KnowledgeContainerIDs.Clear();
            knowledgeContainer.MartialArtSkillUid = null;
            knowledgeContainer.LanguageSkillUid = null;
            var container = knowledgeContainer.KnowledgeContainer;
            if (container is { } && deleteAll)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    PredictedQueueDel(entity);
                }
            }
        }
    }

    public override List<(EntityUid, string)> GetMartialArtsForClientDoohickey(EntityUid knowledgeEntity)
    {
        var clientMartialArts = new List<(EntityUid, string)>();
        var martialArtsList = TryGetKnowledgeWithComp<MartialArtsKnowledgeComponent>(knowledgeEntity);
        foreach (var martialArt in martialArtsList ?? [])
        {
            var knowledgePrototype = MetaData(martialArt.Owner).EntityPrototype?.ID;
            clientMartialArts.Add((martialArt.Owner, Loc.GetString($"knowledge-{knowledgePrototype}")));
        }
        return clientMartialArts;
    }

    public string GetCurrentMastery(Entity<KnowledgeComponent> ent)
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
    public int GetMastery(Entity<KnowledgeComponent> ent)
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

    public int GetMastery(KnowledgeComponent ent)
    {
        return ent.Level switch
        {
            >= 88 => 5,
            >= 76 => 4,
            >= 51 => 3,
            >= 26 => 2,
            >= 1 => 1,
            _ => 0,
        };
    }

    public int GetMastery(EntityUid uid)
    {
        if (TryComp<KnowledgeComponent>(uid, out var comp))
        {
            return comp.Level switch
            {
                >= 88 => 5,
                >= 76 => 4,
                >= 51 => 3,
                >= 26 => 2,
                >= 1 => 1,
                _ => 0,
            };
        }
        else
            return 0;
    }

    public int GetMastery(EntityUid? uid)
    {
        if (uid is { })
        {
            return GetMastery(uid);
        }
        else
            return 0;
    }

    public (int, bool) RollPenetrating(int sides, bool didCritical = false)
    {

        bool isCritical = false;
        int penetratingRolls = 0;
        int currentRoll = _random.Next(1, sides + 1);
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
            currentRoll = _random.Next(1, newSides + 1);
            total += currentRoll - 1;
            isCritical = true;
        }

        return (total, isCritical);
    }

    private void RecursiveRaiseRelayEvent(EntityUid uid, ref KnowledgeContainerRelayEvent ev)
    {
        var enumerator = Transform(uid).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RaiseLocalEvent(child, ref ev);
            RecursiveRaiseRelayEvent(child, ref ev);
        }
    }

    private void EnsureContainer(Entity<KnowledgeContainerComponent> ent)
    {
        ent.Comp.KnowledgeContainer = _container.EnsureContainer<Container>(ent.Owner, KnowledgeContainerComponent.ContainerId);
        // We show the contents of the container to allow knowledge to have visible sprites. I mean, if you really need to show some big brains.
        ent.Comp.KnowledgeContainer.ShowContents = true;
    }

    private void EnsureContainer(Entity<KnowledgeContainerComponent> ent, out Container container)
    {
        container = _container.EnsureContainer<Container>(ent.Owner, KnowledgeContainerComponent.ContainerId);
        // We show the contents of the container to allow knowledge to have visible sprites. I mean, if you really need to show some big brains.
        container.ShowContents = true;

        ent.Comp.KnowledgeContainer = container;
    }
}
