// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Healing;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private EntityQuery<AmputationTraumaComponent> _amputationQuery = default!;
    [Dependency] private EntityQuery<ArmorComponent> _armorQuery = default!;
    [Dependency] private EntityQuery<GodmodeComponent> _godmodeQuery = default!;
    [Dependency] private EntityQuery<TraumaComponent> _traumaQuery = default!;
    [Dependency] private EntityQuery<TraumaInflicterComponent> _query = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    private const string TraumaContainerId = "Traumas";
    // TODO SHITMED: this should be a bool on the trauma entity or something
    public static readonly TraumaType[] TraumasBlockingHealing = { TraumaType.BoneDamage, TraumaType.OrganDamage, TraumaType.Dismemberment };

    public static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    public static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    /// <summary>
    /// Prevent using bruise packs if a part has more than this many bleed stacks from wounds.
    /// Should be replaced by arterial bleeding in the future...
    /// </summary>
    public const float MinBleedToStopHealing = 5f;

    private readonly List<Entity<TraumaComponent>> _traumas = new(8);

    [SubscribeLocalEvent]
    private void OnTraumaInflicterInit(Entity<TraumaInflicterComponent> ent, ref ComponentInit args)
    {
        ent.Comp.TraumaContainer = _container.EnsureContainer<Container>(ent, TraumaContainerId);
    }

    [SubscribeLocalEvent]
    private void OnWoundSeverityPointChanged(
        Entity<TraumaInflicterComponent> wound,
        ref WoundSeverityPointChangedEvent args)
    {
        var part = args.Component.HoldingWoundable;
        if (_godmodeQuery.HasComp(part))
            return;

        // Overflow is only used when we are capping the wound, so we use it over the computed delta
        // which will be useless in this specific scenario.
        var delta = args.Overflow ?? args.NewSeverity - args.OldSeverity;
        if (delta <= 0 || delta < wound.Comp.SeverityThreshold)
            return;

        var woundable = Comp<WoundableComponent>(part);
        var traumasToInduce = RandomTraumaChance((part, woundable), wound, delta);
        if (traumasToInduce.Count <= 0)
            return;

        ApplyTraumas((part, woundable), wound, traumasToInduce, delta);
    }

    [SubscribeLocalEvent]
    private void OnWoundHealAttempt(Entity<TraumaInflicterComponent> ent, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        foreach (var trauma in GetAllWoundTraumas(ent.AsNullable()))
        {
            if (TraumasBlockingHealing.Contains(trauma.Comp.TraumaType))
            {
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage &&
                    _boneQuery.TryComp(args.Woundable, out var bone) &&
                    bone.BoneSeverity != BoneSeverity.Broken)
                    continue;

                args.Cancelled = true;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPartHealAttempt(Entity<WoundableComponent> ent, ref PartHealAttemptEvent args)
    {
        args.Bleeding = ent.Comp.Bleeds > MinBleedToStopHealing;

        var part = ent.AsNullable();
        if (_wound.GetWoundableWounds(part).Any(wound => !_wound.CanHealWound(wound)))
        {
            args.Cancelled = true;
            return;
        }

        if (TraumasBlockingHealing.Any(traumaType => HasWoundableTrauma(ent.AsNullable(), traumaType, false)))
            args.Cancelled = true;
    }

    #region Public API

    public IEnumerable<Entity<TraumaComponent>> GetAllWoundTraumas(Entity<TraumaInflicterComponent?> wound)
    {
        if (!_query.Resolve(wound, ref wound.Comp, false))
            yield break;

        foreach (var trauma in wound.Comp.TraumaContainer.ContainedEntities)
        {
            yield return (trauma, _traumaQuery.Comp(trauma));
        }
    }

    public bool HasAssociatedTrauma(
        EntityUid part,
        Entity<TraumaInflicterComponent?> wound,
        TraumaType? traumaType = null,
        bool showAll = true)
    {
        var boneBroken = showAll && _boneQuery.TryComp(part, out var bone) && bone.BoneSeverity == BoneSeverity.Broken;
        foreach (var trauma in GetAllWoundTraumas(wound))
        {
            if (traumaType != null && trauma.Comp.TraumaType != traumaType)
                continue;

            if (!showAll)
            {
                // TODO: Fill this with other blocking traumas.
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage && !boneBroken)
                    continue;
            }

            return true;
        }

        return false;
    }

    public void AddWoundTraumas(
        Entity<TraumaInflicterComponent?> wound,
        List<Entity<TraumaComponent>> traumas,
        TraumaType? traumaType = null)
    {
        foreach (var trauma in GetAllWoundTraumas(wound))
        {
            if (traumaType != null && trauma.Comp.TraumaType != traumaType)
                continue;

            traumas.Add(trauma);
        }
    }

    public bool HasWoundableTrauma(
        Entity<WoundableComponent?> part,
        TraumaType? traumaType = null,
        bool showAll = true) // Used to skip certain non-lethal traumas like minor bone fractures.
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            if (!_query.TryComp(wound, out var inflicter))
                continue;

            if (HasAssociatedTrauma(part, (wound, inflicter), traumaType, showAll))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get all traumas on a bodypart, optionally of a certain type.
    /// The list is reused between calls, do not store it.
    /// </summary>
    public bool GetPartTraumas(
        Entity<WoundableComponent?> part,
        out List<Entity<TraumaComponent>> traumas,
        TraumaType? traumaType = null)
    {
        traumas = _traumas;
        traumas.Clear();
        AddPartTraumas(part, traumas, traumaType);
        return traumas.Count > 0;
    }

    public void AddPartTraumas(
        Entity<WoundableComponent?> part,
        List<Entity<TraumaComponent>> traumas,
        TraumaType? traumaType = null)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp, false))
            return;

        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_query.TryComp(wound, out var inflicter))
                continue;

            AddWoundTraumas((wound, inflicter), traumas, traumaType);
        }
    }

    /// <summary>
    /// Get all traumas on a body, optionally of a certain type.
    /// The returned list is reused between calls, do not store it.
    /// </summary>
    public List<Entity<TraumaComponent>> GetBodyTraumas(
        Entity<BodyComponent?> body,
        TraumaType? traumaType = null)
    {
        _traumas.Clear();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            AddPartTraumas(part.AsNullable(), _traumas, traumaType);
        }

        return _traumas;
    }

    public List<TraumaType> RandomTraumaChance(
        Entity<WoundableComponent?> part,
        Entity<TraumaInflicterComponent> wound,
        FixedPoint2 severity)
    {
        var traumaList = new List<TraumaType>();
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return traumaList;

        if (severity < wound.Comp.MinTraumaSeverityDelta)
            return traumaList;

        var target = (part, part.Comp);
        if (wound.Comp.AllowedTraumas.Contains(TraumaType.BoneDamage) &&
            RandomBoneTraumaChance(target, wound))
            traumaList.Add(TraumaType.BoneDamage);

        if (wound.Comp.AllowedTraumas.Contains(TraumaType.Dismemberment) &&
            RandomDismembermentTraumaChance(target, wound))
            traumaList.Add(TraumaType.Dismemberment);

        if (wound.Comp.AllowedTraumas.Contains(TraumaType.OrganDamage) &&
            RandomOrganTraumaChance(target, wound))
            traumaList.Add(TraumaType.OrganDamage);

        return traumaList;
    }

    public FixedPoint2 GetArmourChanceDeduction(EntityUid body, Entity<TraumaInflicterComponent> inflicter, TraumaType traumaType, BodyPartType coverage)
    {
        var total = FixedPoint2.Zero;

        foreach (var ent in _inventory.GetHandOrInventoryEntities(body, SlotFlags.WITHOUT_POCKET))
        {
            if (!_armorQuery.TryComp(ent, out var armour))
                continue;

            var deductions = armour.TraumaDeductions;
            var deduction = deductions[traumaType];
            if (!inflicter.Comp.AllowArmourDeduction.Contains(traumaType) || deduction == 0)
                continue;

            var covered = armour.ArmorCoverage;
            if (covered.Contains(coverage))
                total += deduction;
        }

        return total;
    }

    public FixedPoint2 GetTraumaChanceDeduction(
        Entity<TraumaInflicterComponent> wound,
        EntityUid body,
        Entity<WoundableComponent> part,
        FixedPoint2 severity,
        TraumaType traumaType,
        BodyPartType coverage)
    {
        var deduction = part.Comp.TraumaDeductions.GetValueOrDefault(traumaType, FixedPoint2.Zero);
        deduction += GetArmourChanceDeduction(body, wound, traumaType, coverage);
        return deduction;
    }

    public void ApplyMangledTraumas(Entity<WoundableComponent> part,
        Entity<TraumaInflicterComponent?> wound,
        FixedPoint2 severity, EntityUid? user = null)
    {
        if (!_query.Resolve(wound, ref wound.Comp) ||
            wound.Comp.MangledMultipliers == null ||
            !_boneQuery.HasComp(part)) // cant cause bone damage without bone
            return;

        var traumasToInduce = new List<TraumaType>();
        foreach (var traumaType in wound.Comp.MangledMultipliers.Keys)
        {
            if (traumaType == TraumaType.BoneDamage)
            {
                traumasToInduce.Add(TraumaType.BoneDamage);
            }
        }

        ApplyTraumas(part, (wound, wound.Comp), traumasToInduce, severity, user);
    }

    #endregion

    #region Trauma Chance Randoming

    public bool RandomBoneTraumaChance(Entity<WoundableComponent> target, Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target) is not {} partType)
            return false; // Can't sever if already severed

        if (!_boneQuery.TryComp(target, out var bone))
            return false;

        if (bone.BoneSeverity == BoneSeverity.Broken)
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.BoneDamage,
            partType);

        if (deduction == 1)
            return false;

        // We do complete random to get the chance for trauma to happen,
        // We combine multiple parameters and do some math, to get the chance.
        // Even if we get 0.1 damage there's still a chance for injury to be applied, but with the extremely low chance.
        // The more damage, the bigger is the chance.
        var chance = target.Comp.IntegrityCap / (target.Comp.Integrity + bone.BoneIntegrity)
             * _boneTraumaChanceMultipliers[target.Comp.WoundableSeverity]
             - deduction.Float() + woundInflicter.Comp.TraumasChances[TraumaType.BoneDamage];
        return _random.Prob(Math.Clamp((float) chance, 0f, 1f));
    }

    public bool RandomOrganTraumaChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target) is not {} partType)
            return false; // No entity to apply traumas to

        var totalIntegrity = FixedPoint2.Zero;
        foreach (var organ in _part.GetPartOrgans(target.Owner).Values)
        {
            if (!_internalQuery.TryComp(organ, out var organComp))
                continue;

            totalIntegrity += organComp.OrganIntegrity;
        }

        if (totalIntegrity <= 0) // No surviving organs
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.OrganDamage,
            partType);

        if (deduction == 1)
            return false;
        // organ damage is like, very deadly, but not yet
        // so like, like, yeah, we don't want a disabler to induce some EVIL ASS organ damage with a 0,000001% chance and ruin your round
        // Very unlikely to happen if your woundables are in a good condition

        var chance =
            FixedPoint2.Clamp(
                target.Comp.Integrity / target.Comp.IntegrityCap / totalIntegrity
                - deduction + woundInflicter.Comp.TraumasChances[TraumaType.OrganDamage],
                0,
                1);

        return _random.Prob((float) chance);
    }

    public bool RandomDismembermentTraumaChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        // Can't sever if already severed
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target.Owner) is not {} partType ||
            // can't dismember the root part
            !target.Comp.CanRemove ||
            _part.GetParentPart(target.Owner) == null)
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.Dismemberment,
            partType);

        if (deduction == 1)
            return false;

        // Healthy bones decrease the chance of your limb getting delimbed
        var multiplier = 1f;
        if (_boneQuery.TryComp(target, out var bone))
        {
            multiplier = bone.BoneSeverity switch
            {
                BoneSeverity.Normal => 0.3f, // decreases delimb change by 70%
                BoneSeverity.Damaged => 0.6f, // 40%
                BoneSeverity.Cracked => 1f, // 0%,
                BoneSeverity.Broken => 1.2f, // increases by 20%
                _ => 1f
            };
        }

        // TODO SHITMED: this doesnt fucking work?
        float chance = (1f - (MathF.Pow(target.Comp.Integrity.Float(), 1.3f) / target.Comp.IntegrityCap.Float() - 1f)) * multiplier
            - deduction.Float() + woundInflicter.Comp.TraumasChances[TraumaType.Dismemberment].Float();

        // TODO SHITMED: if above is fixed, predicted random
        return _random.Prob(Math.Clamp(chance, 0f, 1f));
    }

    public EntityUid AddTrauma(
        EntityUid target,
        EntityUid part,
        Entity<TraumaInflicterComponent> wound,
        TraumaType traumaType,
        FixedPoint2 severity,
        [ForbidLiteral] ProtoId<OrganCategoryPrototype>? source = null)
    {
        if (TerminatingOrDeleted(wound))
            return EntityUid.Invalid;

        foreach (var trauma in wound.Comp.TraumaContainer.ContainedEntities)
        {
            var containedTraumaComp = _traumaQuery.Comp(trauma);
            if (containedTraumaComp.TraumaType != traumaType)
                continue;

            // Allows us to create multiple dismemberment traumas on the same body part.
            if (source != null && _amputationQuery.CompOrNull(trauma)?.Source != source)
                continue;

            containedTraumaComp.TraumaSeverity = severity;
            return trauma;
        }

        var id = wound.Comp.TraumaPrototypes[traumaType];
        var traumaEnt = PredictedSpawnInContainerOrDrop(id, wound, TraumaContainerId);
        var traumaComp = EnsureComp<TraumaComponent>(traumaEnt);

        traumaComp.TraumaSeverity = severity;
        traumaComp.TraumaTarget = target;
        traumaComp.Wound = wound;
        traumaComp.HoldingWoundable = part;
        Dirty(traumaEnt, traumaComp);

        if (source != null)
        {
            var amputation = EnsureComp<AmputationTraumaComponent>(traumaEnt);
            amputation.Source = source.Value;
            Dirty(traumaEnt, amputation);
        }

        return traumaEnt;
    }

    public void RemoveTraumas(Entity<WoundableComponent?> part, TraumaType type)
    {
        if (!GetPartTraumas(part, out var traumas, type))
            return;

        foreach (var trauma in traumas)
        {
            RemoveTrauma(trauma);
        }
    }

    public void RemoveTrauma(Entity<TraumaComponent> trauma)
    {
        var ev = new TraumaBeingRemovedEvent(trauma);
        RaiseLocalEvent(trauma.Comp.Wound, ref ev);

        PredictedDel(trauma.Owner);
    }

    #endregion

    #region Private API

    private void ApplyTraumas(Entity<WoundableComponent> target, Entity<TraumaInflicterComponent> inflicter,
        List<TraumaType> traumas, FixedPoint2 severity, EntityUid? user = null)
    {
        if (!_organQuery.TryComp(target, out var organ) || organ.Body is not { } body)
            return;

        var category = organ.Category;
        foreach (var trauma in traumas)
        {
            EntityUid? targetChosen = null;
            switch (trauma)
            {
                case TraumaType.BoneDamage:
                    targetChosen = target;
                    break;

                case TraumaType.OrganDamage:
                    var organs = new List<EntityUid>();
                    foreach (var partOrgan in _part.GetPartOrgans(target.Owner).Values)
                    {
                        if (_internalQuery.HasComp(partOrgan))
                            organs.Add(partOrgan);
                    }

                    var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(target), GetNetEntity(inflicter));
                    rand.Shuffle(organs);

                    if (organs.FirstOrNull() is {} chosenOrgan)
                        targetChosen = chosenOrgan;

                    break;
                case TraumaType.Dismemberment:
                    targetChosen = _part.GetParentPart(target.Owner);
                    break;
            }

            if (targetChosen == null)
                continue;

            switch (trauma)
            {
                case TraumaType.BoneDamage:
                    ApplyBoneTrauma(target.Owner, inflicter, severity);
                    break;

                case TraumaType.OrganDamage:
                    var traumaEnt = AddTrauma(targetChosen.Value, target, inflicter, TraumaType.OrganDamage, severity);

                    if (traumaEnt != EntityUid.Invalid
                        && !TryChangeOrganDamageModifier(targetChosen.Value, severity, traumaEnt, "WoundableDamage"))
                    {
                        TryCreateOrganDamageModifier(targetChosen.Value, severity, traumaEnt, "WoundableDamage");
                    }

                    break;

                case TraumaType.Dismemberment:
                    if (_part.GetParentPart(target.Owner) != null && // can't amputate a torso
                        _wound.TryCreateWound(targetChosen.Value, Blunt.Id, 0, out var woundCreated, Brute)) // We need this to add the trauma into.
                    {
                        AddTrauma(
                            targetChosen.Value,
                            targetChosen.Value,
                            (woundCreated.Value.Owner, EnsureComp<TraumaInflicterComponent>(woundCreated.Value.Owner)),
                            TraumaType.Dismemberment,
                            severity,
                            source: category);

                        _wound.AmputateWoundable(targetChosen.Value, target.AsNullable(), user);
                    }
                    break;
            }

        }

        // TODO: veins, would have been very lovely to integrate this into vascular system
        //if (RandomVeinsTraumaChance(woundable))
        //{
        //    traumaApplied = ApplyDamageToVeins(woundable.Veins!.ContainedEntities[0], severity * _veinsDamageMultipliers[woundable.WoundableSeverity]);
        //    _sawmill.Info(traumaApplied
        //        ? $"A new trauma (Raw Severity: {severity}) was created on target: {target} of type Vein damage"
        //        : $"Tried to create a trauma on target: {target}, but no trauma was applied. Type: Vein damage.");
        //}
    }


    #endregion
}
