using System.Diagnostics.CodeAnalysis;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Body;
using Content.Shared.Construction;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
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
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly BodySystem _body = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeLanguage();
        InitializeMartialArts();
        InitializeOnWear();

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentShutdown>(OnKnowledgeContainerShutdown);
        SubscribeLocalEvent<KnowledgeContainerComponent, EntGotInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<KnowledgeContainerComponent, EntGotRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<KnowledgeContainerComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEvent);

        SubscribeLocalEvent<KnowledgeContainerComponent, ComponentInit>(OnComponentInit);

        //Experience Methods
        SubscribeLocalEvent<KnowledgeHolderComponent, AddExperience>(OnAddExperience);
    }

    private void OnKnowledgeContainerShutdown(Entity<KnowledgeContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.KnowledgeContainer is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnEntInserted(Entity<KnowledgeContainerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var body = _body.GetBody(ent);
        if (!TryComp<KnowledgeHolderComponent>(body, out var knowledgeHolder))
            return;
        knowledgeHolder.KnowledgeEntity = ent;
        Dirty(ent);
    }

    private void OnEntRemoved(Entity<KnowledgeContainerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var body = _body.GetBody(ent);
        if (!TryComp<KnowledgeHolderComponent>(body, out var knowledgeHolder))
            return;
        knowledgeHolder.KnowledgeEntity = null;
        Dirty(ent);
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

        while (current.IsValid() && TryComp(current, out TransformComponent? xform))
        {
            if (xform.ParentUid == potentialParent)
                return true;

            current = xform.ParentUid;

            if (HasComp<MapComponent>(current) || HasComp<MapGridComponent>(current))
                break;
        }

        return false;
    }

    public void OnInit(Entity<KnowledgeHolderComponent> ent)
    {
        var knowledgeContainer = EnsureKnowledgeContainer(ent);
        ent.Comp.KnowledgeEntity = knowledgeContainer.Owner;
        Dirty(ent.Owner, ent.Comp);

        if (TryComp<LanguageSpeakerComponent>(ent.Owner, out var languageSpeaker))
            UpdateEntityLanguages((ent, languageSpeaker));

    }

    public void OnAddExperience(Entity<KnowledgeHolderComponent> ent, ref AddExperience args)
    {
        if (TryGetKnowledgeUnit(ent, args.KnowledgeType) is not { } knowledgeUnit || !TryComp<KnowledgeComponent>(knowledgeUnit, out var knowledgeComponent))
        {
            if (_random.Prob(0.2f))
                TryAddKnowledgeUnit(ent, (args.KnowledgeType, 0));
            return;
        }

        var knowledge = (knowledgeUnit, knowledgeComponent);

        if (_timing.CurTick.Value < knowledgeComponent.LastExperienceTick + (uint) (1.0f * _timing.TickRate))
            return;

        knowledgeComponent.LastExperienceTick = _timing.CurTick.Value;

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
                    _popup.PopupEntity(Loc.GetString("knowledge-level-epiphany", ("knowledge", Loc.GetString(PopupString(knowledgeUnit)))), ent, ent, PopupType.Medium);
            }
        }
        if (knowledgeComponent.Level > 100)
            knowledgeComponent.Level = 100;
        if (getMastery != GetMastery(knowledge))
        {
            var knowledgePrototype = MetaData(knowledgeUnit).EntityPrototype?.ID;
            _popup.PopupEntity(Loc.GetString("knowledge-level-up-popup", ("knowledge", Loc.GetString(PopupString(knowledgeUnit))), ("mastery", GetMasteryString(knowledge).ToLower())), ent, ent, PopupType.Medium);
        }
        Dirty(ent);
    }

    public override (string Category, KnowledgeInfo Info) GetKnowledgeInfo(Entity<KnowledgeComponent> ent)
    {
        var category = _protoMan.Index(ent.Comp.Category);

        var knowledgeInfo = new KnowledgeInfo("", "", ent.Comp.Color, ent.Comp.Sprite);
        var knowledgePrototype = MetaData(ent).EntityPrototype?.ID;
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

        var ent = EnsureKnowledgeContainer((target, holderComponent));
        var container = EnsureContainer(ent);

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
            if (_netManager.IsClient)
                return knowledgeEnt;

            var result = PredictedTrySpawnInContainer(knowledgeId.Item1, ent.Owner, KnowledgeContainerComponent.ContainerId, out var knowledgeUnit);
            if (!result || knowledgeUnit is not { } knowledgeUnitVerified)
                return knowledgeEnt;
            if (TryComp<KnowledgeComponent>(knowledgeUnitVerified, out var knowledgeComp))
            {
                knowledgeComp.Level = knowledgeId.Item2;
                knowledgeEnt = (knowledgeUnitVerified, knowledgeComp);
                Dirty(knowledgeUnitVerified, knowledgeComp);
            }
            ent.Comp.KnowledgeContainerIDs[knowledgeId.Item1] = knowledgeUnitVerified;
            if (TryComp<LanguageKnowledgeComponent>(knowledgeUnitVerified, out var languageComp))
            {
                EnsureComp<LanguageSpeakerComponent>(target);
                _popup.PopupEntity(Loc.GetString("knowledge-unit-learned-popup", ("knowledge", Loc.GetString($"{languageComp.LanguageId.Id}"))), target, target, PopupType.Medium);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("knowledge-unit-learned-popup", ("knowledge", Loc.GetString($"knowledge-{knowledgeId.Item1.ToString()}"))), target, target, PopupType.Medium);

            }
        }
        Dirty(ent);
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
    public override Entity<KnowledgeComponent>? TryGetKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit)
    {
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent) || TryGetKnowledgeEntity((target, holderComponent)) is not { } ent || !TryComp<KnowledgeContainerComponent>(ent, out var comp))
            return null;

        if (comp.KnowledgeContainerIDs.TryGetValue(knowledgeUnit, out var knowledge) && TryComp<KnowledgeComponent>(knowledge, out var knowledgeComponent))
            return (knowledge, knowledgeComponent);
        else
            return null;
    }

    /// <summary>
    /// Returns all knowledge units inside the container component.
    /// </summary>
    public override List<Entity<KnowledgeComponent>>? TryGetAllKnowledgeUnits(EntityUid target)
    {
        List<Entity<KnowledgeComponent>>? found = null;
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent))
            return found;

        var ent = EnsureKnowledgeContainer((target, holderComponent));
        var container = EnsureContainer(ent);

        if (container == null)
            return null;

        foreach (var unit in container.ContainedEntities)
        {
            if (!TryComp<KnowledgeComponent>(unit, out var knowledgeComp))
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
        if (!TryComp<KnowledgeHolderComponent>(target, out var holderComponent))
            return null;

        var ent = EnsureKnowledgeContainer((target, holderComponent));
        var container = EnsureContainer(ent);

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

        var ent = EnsureKnowledgeContainer((target, holderComponent));
        var container = EnsureContainer(ent);

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
        var list = _body.GetOrgans<KnowledgeContainerComponent>(ent.Owner);

        foreach (var organ in list)
        {
            ent.Comp.KnowledgeEntity = organ;
            Dirty(ent.Owner, ent.Comp);
            return organ;
        }

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
        if (TryComp<KnowledgeHolderComponent>(uid, out var knowledgeHolder) && knowledgeHolder.KnowledgeEntity is { })
            return knowledgeHolder.KnowledgeEntity;

        return null;
    }

    public override EntityUid? TryGetKnowledgeEntity(Entity<KnowledgeHolderComponent> ent)
    {
        if (ent.Comp.KnowledgeEntity is { })
            return ent.Comp.KnowledgeEntity;

        return TryGetKnowledgeContainer(ent);
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

    public override List<(EntityUid, string)> GetMartialArtsForClientDoohickey(EntityUid target)
    {
        var clientMartialArts = new List<(EntityUid, string)>();
        var martialArtsList = TryGetKnowledgeWithComp<MartialArtsKnowledgeComponent>(target);
        foreach (var martialArt in martialArtsList ?? [])
        {
            var knowledgePrototype = MetaData(martialArt.Owner).EntityPrototype?.ID;
            clientMartialArts.Add((martialArt.Owner, Loc.GetString($"knowledge-{knowledgePrototype}")));
        }
        return clientMartialArts;
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

    public int GetMastery(EntityUid? uid)
    {
        if (uid is { } validUid)
            return GetMastery(validUid);
        return 0;
    }
    public override float SharpCurve(Entity<KnowledgeComponent> knowledge)
    {
        return ((float) knowledge.Comp.Level / 100.0f) * ((float) knowledge.Comp.Level / 100.0f);
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

    private Container EnsureContainer(Entity<KnowledgeContainerComponent> ent)
    {
        if (ent.Comp.KnowledgeContainer != null)
            return ent.Comp.KnowledgeContainer;

        ent.Comp.KnowledgeContainer = _container.EnsureContainer<Container>(ent.Owner, KnowledgeContainerComponent.ContainerId);
        // We show the contents of the container to allow knowledge to have visible sprites. I mean, if you really need to show some big brains.
        ent.Comp.KnowledgeContainer.ShowContents = true;

        return ent.Comp.KnowledgeContainer;
    }

    private void OnComponentInit(Entity<KnowledgeContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.KnowledgeContainer = _container.EnsureContainer<Container>(ent.Owner, KnowledgeContainerComponent.ContainerId);

        ent.Comp.KnowledgeContainer.ShowContents = true;
        Dirty(ent);
    }

    private string PopupString(EntityUid knowledgeUnit)
    {
        if (TryComp<LanguageKnowledgeComponent>(knowledgeUnit, out var knowledgeComp))
            return $"{knowledgeComp.LanguageId.Id}";
        else
            return $"knowledge-{MetaData(knowledgeUnit).EntityPrototype?.ID}";
    }
}
