using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.Mind;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using YamlDotNet.Core.Tokens;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// This handles all knowledge related entities.
/// </summary>
public sealed partial class KnowledgeSystem : CommonKnowledgeSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;


    public static readonly EntProtoId LanguageKnowledgeId = "LanguageKnowledge";
    private EntityQuery<KnowledgeComponent> _knowledgeQuery;
    private EntityQuery<KnowledgeContainerComponent> _containerQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentShutdown>(OnKnowledgeContainerShutdown);
        SubscribeLocalEvent<KnowledgeContainerComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<KnowledgeContainerComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEvent);
        SubscribeLocalEvent<BodyComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEventBodyPart);
        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnSpeakerInit);
        SubscribeLocalEvent<LanguageSpeakerComponent, AddLanguageEvent>(OnLanguageAdded);
        SubscribeLocalEvent<LanguageSpeakerComponent, RemoveLanguageEvent>(OnLanguageRemoved);
        SubscribeLocalEvent<LanguageSpeakerComponent, UpdateLanguageEvent>(OnLanguageUpdated);

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

    public void OnConstructionGetGroupEventBodyPart(Entity<BodyComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        foreach (var part in _body.GetBodyOrgans(ent))
        {
            if (TryComp<KnowledgeContainerComponent>(part.Id, out var knowledgeContainer))
            {
                RaiseLocalEvent(part.Id, ref args);
                return;
            }
        }
    }

    public void OnConstructionGetGroupEvent(Entity<KnowledgeContainerComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        if (TryGetKnowledgeWithComp<ConstructionKnowledgeComponent>(ent) is not { } knowledge)
            return;

        foreach (var (_, comp, _) in knowledge)
        {
            args.Groups.Add(comp.Group);
        }
    }

    public void OnSpeakerInit(Entity<LanguageSpeakerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<LanguageSpeakerComponent>(ent, out var languageSpeakerComponent))
            return;


        if (TryGetKnowledgeEntity(ent) is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainer))
            return;
        //Log.Error($"Entity {ToPrettyString(ent)} failed to setup {nameof(KnowledgeContainerComponent)} properly!");

        if (knowledgeContainer.KnowledgeContainer == null)
            knowledgeContainer.KnowledgeContainer = _container.MakeContainer<Container>(knowledgeEnt, LanguageKnowledgeId);

        Log.Debug($"Entity {ToPrettyString(ent)} has {languageSpeakerComponent.SpokenLanguages.Count()} speaks and {languageSpeakerComponent.UnderstoodLanguages.Count()} understands.");
        foreach (var spoken in languageSpeakerComponent.SpokenLanguages)
        {
            EntityUid entity = Spawn("LanguageKnowledge");
            if (TryComp<LanguageKnowledgeComponent>(entity, out var langComp))
            {
                langComp.LanguageId = spoken;
                langComp.Speaks = true;

                if (languageSpeakerComponent.UnderstoodLanguages.Contains(spoken))
                    langComp.Understands = true;

                Dirty(entity, langComp);
                _container.Insert(entity, knowledgeContainer.KnowledgeContainer);
            }
        }

        foreach (var understood in languageSpeakerComponent.UnderstoodLanguages.Except(languageSpeakerComponent.SpokenLanguages))
        {
            EntityUid entity = Spawn("LanguageKnowledge");
            if (TryComp<LanguageKnowledgeComponent>(entity, out var langComp))
            {
                langComp.LanguageId = understood;
                langComp.Understands = true;
                Dirty(entity, langComp);
                _container.Insert(entity, knowledgeContainer.KnowledgeContainer);
            }
        }

        UpdateEntityLanguages(ent);
    }

    public void OnLanguageAdded(Entity<LanguageSpeakerComponent> ent, ref AddLanguageEvent args)
    {
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(ent, out var knowledge) && knowledge.KnowledgeContainer != null)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt);

            if (knownLanguages != null)
            {
                EntityUid? languageToAdd = null;
                foreach (var language in knownLanguages)
                {
                    if (language.Comp1.LanguageId == args.Language)
                    {
                        languageToAdd = language;
                        break;
                    }
                }

                if (languageToAdd == null)
                {
                    languageToAdd = Spawn("LanguageKnowledge");
                }
                if (TryComp<LanguageKnowledgeComponent>(languageToAdd, out var langComp))
                {
                    langComp.LanguageId = args.Language;
                    langComp.Understands = args.AddUnderstood;
                    langComp.Speaks = args.AddSpoken;
                    Dirty(languageToAdd.Value, langComp);
                    _container.Insert(languageToAdd.Value, knowledge.KnowledgeContainer);
                }
            }

            else
            {
                EntityUid entity = Spawn("LanguageKnowledge");
                if (TryComp<LanguageKnowledgeComponent>(entity, out var langComp))
                {
                    langComp.LanguageId = args.Language;
                    langComp.Understands = args.AddUnderstood;
                    langComp.Speaks = args.AddSpoken;
                    Dirty(entity, langComp);
                    _container.Insert(entity, knowledge.KnowledgeContainer);
                }
            }
            Dirty(ent);
            UpdateEntityLanguages(ent);
        }
    }

    public void OnLanguageRemoved(Entity<LanguageSpeakerComponent> ent, ref RemoveLanguageEvent args)
    {

        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(ent, out var knowledge) && knowledge.KnowledgeContainer != null)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt);

            if (knownLanguages != null)
            {
                foreach (var language in knownLanguages)
                {
                    if (language.Comp1.LanguageId == args.Language)
                    {
                        if (args.RemoveSpoken && args.RemoveUnderstood)
                        {
                            _container.Remove(language.Owner, knowledge.KnowledgeContainer);
                            PredictedQueueDel(language.Owner);
                        }
                        else
                        {
                            language.Comp1.Speaks = !args.RemoveSpoken;
                            language.Comp1.Understands = !args.RemoveSpoken;
                            Dirty(language.Owner, language.Comp1);
                        }
                        // We don't ensure that the entity has a speaker comp. If it doesn't... Well, woe be the caller of this method.
                        UpdateEntityLanguages(ent);
                        return;
                    }
                }
            }
        }
    }

    public void OnLanguageUpdated(Entity<LanguageSpeakerComponent> ent, ref UpdateLanguageEvent args)
    {
        UpdateEntityLanguages(ent);
    }

    public void UpdateEntityLanguages(Entity<LanguageSpeakerComponent> ent)
    {
        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt && TryComp<LanguageKnowledgeComponent>(ent, out var knowledgeEntity)) // Trauma edit
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt);

            if (knownLanguages != null)
            {
                foreach (var language in knownLanguages)
                {
                    if (language.Comp1.Speaks == true)
                        ev.SpokenLanguages.Add(language.Comp1.LanguageId);
                    if (language.Comp1.Understands == true)
                        ev.UnderstoodLanguages.Add(language.Comp1.LanguageId);
                }
            }
        }
        else
        {
            // Fallback for anything that doesn't have a knowledge component.
            foreach (var spoken in ent.Comp.SpokenLanguages)
            {
                ev.SpokenLanguages.Add(spoken);
            }
            foreach (var understood in ent.Comp.SpokenLanguages)
            {
                ev.UnderstoodLanguages.Add(understood);
            }
        }

        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpokenLanguages.Clear();
        ent.Comp.UnderstoodLanguages.Clear();

        ent.Comp.SpokenLanguages.AddRange(ev.SpokenLanguages);
        ent.Comp.UnderstoodLanguages.AddRange(ev.UnderstoodLanguages);

        _language.EnsureValidLanguage(ent);

        Dirty(ent);
    }

    public EntityUid? TryGetKnowledgeEntity(Entity<LanguageSpeakerComponent> ent)
    {
        if (TryComp<KnowledgeContainerComponent>(ent, out var knowledgeContainer1))
        {
            return ent.Owner;
        }
        foreach (var part in _body.GetBodyOrgans(ent))
        {
            if (TryComp<KnowledgeContainerComponent>(part.Id, out var knowledgeContainer))
            {
                return part.Id;
            }
        }
        return null;
    }

    public override (string Category, KnowledgeInfo Info) GetKnowledgeInfo(Entity<KnowledgeComponent> knowledge)
    {
        var (uid, comp) = knowledge;
        var category = _protoMan.Index(comp.Category);

        var ev = new KnowledgeGetDescriptionEvent();
        RaiseLocalEvent(uid, ref ev);
        var description = ev.Description ?? Description(uid);
        var knowledgeInfo = new KnowledgeInfo("Blank", "Blank", comp.Color, comp.Sprite);
        if (TryComp<LanguageKnowledgeComponent>(uid, out var languageKnowledge))
        {
            knowledgeInfo.Name = languageKnowledge.Speaks
                ? "Speaks "
                : "";
            if (languageKnowledge.Speaks && languageKnowledge.Understands)
            {
                knowledgeInfo.Name += "and ";
            }
            knowledgeInfo.Name += languageKnowledge.Understands
                ? knowledgeInfo.Name + "Understands "
                : knowledgeInfo.Name;
            //knowledgeInfo.Name += Loc.GetString(_protoMan.Index<LanguagePrototype>(languageKnowledge.LanguageId).Name);
            knowledgeInfo.Name += languageKnowledge.LanguageId.ToString();
        }
        else if (TryComp<ConstructionKnowledgeComponent>(uid, out var constructionKnowledge))
        {
            knowledgeInfo.Name = constructionKnowledge.Group.ToString();
        }

        return (Loc.GetString(category.Name),
            knowledgeInfo);
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
    public override bool TryAddKnowledgeUnit(EntityUid target, EntProtoId knowledgeId)
    {
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent);

        if (HasKnowledgeUnit(ent.Owner, knowledgeId) is { } uid)
            return false;

        return PredictedTrySpawnInContainer(knowledgeId, ent.Owner, KnowledgeContainerComponent.ContainerId, out _);
    }

    /// <inheritdoc cref="TryAddKnowledgeUnit(Robust.Shared.GameObjects.EntityUid,Robust.Shared.Prototypes.EntProtoId)"/>
    public override bool TryAddKnowledgeUnit(
        EntityUid target,
        EntProtoId knowledgeId,
        [NotNullWhen(true)] out EntityUid? found)
    {
        found = null;
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent);

        if (HasKnowledgeUnit(ent.Owner, knowledgeId) is { } uid)
            return false;

        return PredictedTrySpawnInContainer(knowledgeId, ent.Owner, KnowledgeContainerComponent.ContainerId, out found);
    }

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public override void AddKnowledgeUnits(EntityUid target, List<EntProtoId> knowledgeList)
    {
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent);

        foreach (var knowledgeId in knowledgeList)
        {
            if (HasKnowledgeUnit(ent.Owner, knowledgeId) is { } uid)
                continue;

            PredictedTrySpawnInContainer(knowledgeId, ent.Owner, KnowledgeContainerComponent.ContainerId, out _);
        }
    }

    /// <summary>
    /// Removes a knowledge unit from a container. This version takes into account levels and categories of knowledge.
    /// If knowledge has higher level than specified in the method, or a different category, it will not be removed.
    /// </summary>
    /// <param name="target">Entity to remove a unit from.</param>
    /// <param name="knowledgeUnit">Knowledge unit to remove.</param>
    /// <param name="category">Category of knowledge that we are removing.</param>
    /// <param name="level">Level of removal, that will remove knowledge only if its level is lower or equal to that value.</param>
    /// <param name="force">If true, will override all checks and will just always remove this knowledge.</param>
    /// <returns>True if removed successfully.</returns>
    public override EntityUid? TryRemoveKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit, ProtoId<KnowledgeCategoryPrototype> category, int level, bool force = false)
    {
        if (TryGetKnowledgeUnit(target, knowledgeUnit) is { } unit)
        {
            if ((_knowledgeQuery.TryComp(unit, out var knowledge) && knowledge != null) && CanRemoveKnowledge((unit, knowledge), category, level, force) is { })
            {
                PredictedQueueDel(unit);
                return target;
            }
        }
        return null;
    }

    /// <summary>
    /// Removes a knowledge unit from a container. Will not remove a knowledge unit if it's marked as unremoveable,
    /// unless force parameter is true.
    /// </summary>
    public override EntityUid? TryRemoveKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit, bool force = false)
    {
        if (TryGetKnowledgeUnit(target, knowledgeUnit) is not { } unit
            || !_knowledgeQuery.TryComp(unit, out var knowledge))
            return null;

        if (!force && knowledge.Unremoveable)
            return null;

        PredictedQueueDel(unit);
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
    /// False if the target is not a knowledge container, or if knowledge unit wasn't found.
    /// </returns>
    public override EntityUid? TryGetKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit)
    {
        EntityUid? found = null;
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent, out var container);

        foreach (var unit in container.ContainedEntities)
        {
            var prototype = Prototype(unit)?.ID;
            if (prototype is null
                || prototype != knowledgeUnit)
                continue;

            found = unit;
            break;
        }

        return found;
    }

    /// <summary>
    /// Checks if that knowledge unit already exists inside a knowledge container.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container, and true if knowledge unit with that ID was found.
    /// </returns>
    public override EntityUid? HasKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit)
    {
        EnsureKnowledgeContainer(target, out var ent);
        EnsureContainer(ent, out var container);

        foreach (var unit in container.ContainedEntities)
        {
            var prototype = Prototype(unit)?.ID;
            if (prototype is null
                || prototype != knowledgeUnit)
                continue;

            return target;
        }

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
