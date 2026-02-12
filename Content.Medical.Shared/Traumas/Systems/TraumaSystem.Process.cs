// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Healing;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Pain;
using Content.Medical.Shared.Wounds;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    private EntityQuery<ArmorComponent> _armorQuery;

    private const string TraumaContainerId = "Traumas";
    // TODO SHITMED: this should be a bool on the trauma entity or something
    public static readonly TraumaType[] TraumasBlockingHealing = { TraumaType.BoneDamage, TraumaType.OrganDamage, TraumaType.Dismemberment };

    public static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    public static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    private void InitProcess()
    {
        _armorQuery = GetEntityQuery<ArmorComponent>();

        SubscribeLocalEvent<TraumaInflicterComponent, ComponentInit>(OnTraumaInflicterInit);
        SubscribeLocalEvent<TraumaComponent, ComponentGetState>(OnComponentGet);
        SubscribeLocalEvent<TraumaComponent, ComponentHandleState>(OnComponentHandleState);
        SubscribeLocalEvent<TraumaInflicterComponent, WoundSeverityPointChangedEvent>(OnWoundSeverityPointChanged);
        SubscribeLocalEvent<TraumaInflicterComponent, WoundHealAttemptEvent>(OnWoundHealAttempt);

        SubscribeLocalEvent<WoundableComponent, PartHealAttemptEvent>(OnPartHealAttempt);
    }

    private void OnTraumaInflicterInit(
        Entity<TraumaInflicterComponent> woundEnt,
        ref ComponentInit args)
    {
        woundEnt.Comp.TraumaContainer = _container.EnsureContainer<Container>(woundEnt, TraumaContainerId);
    }

    private void OnComponentGet(EntityUid uid, TraumaComponent comp, ref ComponentGetState args)
    {
        var state = new TraumaComponentState
        {
            TraumaTarget = GetNetEntity(comp.TraumaTarget),
            HoldingWoundable = GetNetEntity(comp.HoldingWoundable),
            TargetType = comp.TargetType,
            TraumaType = comp.TraumaType,
            TraumaSeverity = comp.TraumaSeverity,
        };

        args.State = state;
    }

    private void OnComponentHandleState(EntityUid uid, TraumaComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TraumaComponentState state)
            return;

        component.TraumaTarget = GetEntity(state.TraumaTarget);
        component.HoldingWoundable = GetEntity(state.HoldingWoundable);
        component.TargetType = state.TargetType;
        component.TraumaType = state.TraumaType;
        component.TraumaSeverity = state.TraumaSeverity;
    }


    private void OnWoundSeverityPointChanged(
        Entity<TraumaInflicterComponent> woundEnt,
        ref WoundSeverityPointChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted
            || HasComp<GodmodeComponent>(args.Component.HoldingWoundable))
            return;

        // Overflow is only used when we are capping the wound, so we use it over the computed delta
        // which will be useless in this specific scenario.
        var delta = args.Overflow ?? args.NewSeverity - args.OldSeverity;
        if (delta <= 0 || delta < woundEnt.Comp.SeverityThreshold)
            return;

        var traumasToInduce = RandomTraumaChance(args.Component.HoldingWoundable, woundEnt, delta);
        if (traumasToInduce.Count <= 0)
            return;

        var woundable = args.Component.HoldingWoundable;
        var woundableComp = Comp<WoundableComponent>(args.Component.HoldingWoundable);
        ApplyTraumas((woundable, woundableComp), woundEnt, traumasToInduce, delta);
    }

    private void OnWoundHealAttempt(Entity<TraumaInflicterComponent> inflicter, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        foreach (var trauma in GetAllWoundTraumas(inflicter, inflicter))
        {
            if (TraumasBlockingHealing.Contains(trauma.Comp.TraumaType))
            {
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage
                    && GetBone(args.Woundable.AsNullable()) is {} bone
                    && bone.Comp.BoneSeverity != BoneSeverity.Broken)
                    continue;

                args.Cancelled = true;
            }
        }
    }

    private void OnPartHealAttempt(Entity<WoundableComponent> ent, ref PartHealAttemptEvent args)
    {
        args.Bleeding = ent.Comp.Bleeds > 0;
        if (TraumasBlockingHealing.Any(traumaType => HasWoundableTrauma(ent, traumaType, ent.Comp, false)))
            args.Cancelled = true;
    }

    #region Public API

    public IEnumerable<Entity<TraumaComponent>> GetAllWoundTraumas(
        EntityUid woundInflicter,
        TraumaInflicterComponent? component = null)
    {
        if (!Resolve(woundInflicter, ref component, false))
            yield break;

        foreach (var trauma in component.TraumaContainer.ContainedEntities)
        {
            yield return (trauma, Comp<TraumaComponent>(trauma));
        }
    }

    public bool HasAssociatedTrauma(
        EntityUid woundable,
        EntityUid woundInflicter,
        WoundableComponent? woundableComp = null,
        TraumaType? traumaType = null,
        TraumaInflicterComponent? component = null,
        bool showAll = true)
    {
        if (!Resolve(woundInflicter, ref component, false)
            || !Resolve(woundable, ref woundableComp, false))
            return false;

        foreach (var trauma in GetAllWoundTraumas(woundInflicter, component))
        {
            if (trauma.Comp.TraumaTarget == null)
                continue;

            if (trauma.Comp.TraumaType != traumaType && traumaType != null)
                continue;

            if (!showAll)
            {
                // TODO: Fill this with other blocking traumas.
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage
                    && (GetBone((woundable, woundableComp)) is not {} bone
                    || bone.Comp.BoneSeverity != BoneSeverity.Broken))
                    continue;
            }

            return true;
        }

        return false;
    }

    public bool TryGetAssociatedTrauma(
        EntityUid woundInflicter,
        [NotNullWhen(true)] out List<Entity<TraumaComponent>>? traumas,
        TraumaType? traumaType = null,
        TraumaInflicterComponent? component = null)
    {
        traumas = null;
        if (!Resolve(woundInflicter, ref component, false))
            return false;

        traumas = new List<Entity<TraumaComponent>>();
        foreach (var trauma in GetAllWoundTraumas(woundInflicter, component))
        {
            if (trauma.Comp.TraumaTarget == null)
                continue;

            if (trauma.Comp.TraumaType != traumaType && traumaType != null)
                continue;

            traumas.Add(trauma);
        }

        return true;
    }

    public bool HasWoundableTrauma(
        EntityUid woundable,
        TraumaType? traumaType = null,
        WoundableComponent? woundableComp = null,
        bool showAll = true) // Used to skip certain non-lethal traumas like minor bone fractures.
    {
        if (!Resolve(woundable, ref woundableComp, false))
            return false;

        foreach (var woundEnt in _wound.GetWoundableWounds(woundable, woundableComp))
        {
            if (!TryComp<TraumaInflicterComponent>(woundEnt, out var inflicterComp))
                continue;

            if (HasAssociatedTrauma(woundable, woundEnt, woundableComp, traumaType, inflicterComp, showAll))
                return true;
        }

        return false;
    }

    public bool TryGetWoundableTrauma(
        EntityUid woundable,
        [NotNullWhen(true)] out List<Entity<TraumaComponent>>? traumas,
        TraumaType? traumaType = null,
        WoundableComponent? woundableComp = null)
    {
        traumas = null;
        if (!Resolve(woundable, ref woundableComp, false))
            return false;

        traumas = new List<Entity<TraumaComponent>>();
        foreach (var woundEnt in _wound.GetWoundableWounds(woundable, woundableComp))
        {
            if (!TryComp<TraumaInflicterComponent>(woundEnt, out var inflicterComp))
                continue;

            if (TryGetAssociatedTrauma(woundEnt, out var traumasFound, traumaType, inflicterComp))
                traumas.AddRange(traumasFound);
        }

        return traumas.Count > 0;
    }

    public bool HasBodyTrauma(
        Entity<BodyComponent?> body,
        TraumaType? traumaType = null)
    {
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (HasWoundableTrauma(part, traumaType, part.Comp))
                return true;
        }

        return false;
    }

    public List<Entity<TraumaComponent>> GetBodyTraumas(
        Entity<BodyComponent?> body,
        TraumaType? traumaType = null)
    {
        var traumas = new List<Entity<TraumaComponent>>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (TryGetWoundableTrauma(part, out var traumasFound, traumaType, part.Comp))
                traumas.AddRange(traumasFound);
        }

        return traumas;
    }

    public List<TraumaType> RandomTraumaChance(
        EntityUid target,
        Entity<TraumaInflicterComponent> woundInflicter,
        FixedPoint2 severity,
        WoundableComponent? woundable = null)
    {
        var traumaList = new List<TraumaType>();
        if (!Resolve(target, ref woundable, false))
            return traumaList;


        if (severity > 5 && woundInflicter.Comp.AllowedTraumas.Contains(TraumaType.NerveDamage) &&
            RandomNerveDamageChance((target, woundable), woundInflicter))
            traumaList.Add(TraumaType.NerveDamage);

        if (severity > 10 && woundInflicter.Comp.AllowedTraumas.Contains(TraumaType.BoneDamage) &&
            RandomBoneTraumaChance((target, woundable), woundInflicter))
            traumaList.Add(TraumaType.BoneDamage);

        if (severity > 10 && woundInflicter.Comp.AllowedTraumas.Contains(TraumaType.Dismemberment) &&
            RandomDismembermentTraumaChance((target, woundable), woundInflicter))
            traumaList.Add(TraumaType.Dismemberment);

        if (severity > 15 && woundInflicter.Comp.AllowedTraumas.Contains(TraumaType.OrganDamage) &&
            RandomOrganTraumaChance((target, woundable), woundInflicter))
            traumaList.Add(TraumaType.OrganDamage);

        //if (RandomVeinsTraumaChance(woundable))
        //    traumaList.Add(TraumaType.VeinsDamage);

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
        Entity<TraumaInflicterComponent> inflicter,
        EntityUid body,
        Entity<WoundableComponent> traumaTarget,
        FixedPoint2 severity,
        TraumaType traumaType,
        BodyPartType coverage)
    {
        var deduction = traumaTarget.Comp.TraumaDeductions.GetValueOrDefault(traumaType, FixedPoint2.Zero);
        deduction += GetArmourChanceDeduction(body, inflicter, traumaType, coverage);

        var traumaDeductionEvent = new TraumaChanceDeductionEvent(severity, traumaType, 0);
        RaiseLocalEvent(traumaTarget, ref traumaDeductionEvent);

        deduction += traumaDeductionEvent.ChanceDeduction;

        return deduction;
    }

    public void ApplyMangledTraumas(EntityUid woundable,
        EntityUid wound,
        FixedPoint2 severity,
        WoundableComponent? woundableComp = null,
        TraumaInflicterComponent? inflicterComponent = null)
    {
        if (!Resolve(wound, ref inflicterComponent, false)
            || !Resolve(woundable, ref woundableComp, false)
            || inflicterComponent.MangledMultipliers == null)
            return;

        var traumasToInduce = new List<TraumaType>();
        foreach (var traumaType in inflicterComponent.MangledMultipliers.Keys)
        {
            switch (traumaType)
            {
                case TraumaType.BoneDamage:
                    {
                        if (GetBone((woundable, woundableComp)) == null)
                            break;

                        traumasToInduce.Add(TraumaType.BoneDamage);
                        break;
                    }
            }
        }

        ApplyTraumas((woundable, woundableComp), (wound, inflicterComponent), traumasToInduce, severity);
    }

    #endregion

    #region Trauma Chance Randoming

    public bool RandomBoneTraumaChance(Entity<WoundableComponent> target, Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target) is not {} partType)
            return false; // Can't sever if already severed

        if (GetBone(target.AsNullable()) is not {} bone)
            return false;

        if (bone.Comp.BoneSeverity == BoneSeverity.Broken)
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
        var chance = FixedPoint2.Clamp(
            target.Comp.IntegrityCap / (target.Comp.WoundableIntegrity + bone.Comp.BoneIntegrity)
             * _boneTraumaChanceMultipliers[target.Comp.WoundableSeverity]
             - deduction + woundInflicter.Comp.TraumasChances[TraumaType.BoneDamage],
            0,
            1);

        return _random.Prob((float) chance);
    }

    public bool RandomNerveDamageChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target) is not {} partType)
            return false; // No entity to apply pain to

        if (!TryComp<NerveComponent>(target, out var nerve))
            return false;

        if (nerve.PainFeels < 0.2)
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.NerveDamage,
            partType);

        if (deduction == 1)
            return false;
        // literally dismemberment chance, but lower by default
        var chance =
            FixedPoint2.Clamp(
                target.Comp.WoundableIntegrity / target.Comp.IntegrityCap / 20
                - deduction + woundInflicter.Comp.TraumasChances[TraumaType.NerveDamage],
                0,
                1);

        return _random.Prob((float) chance);
    }

    public bool RandomOrganTraumaChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target) is not {} partType)
            return false; // No entity to apply pain to

        var totalIntegrity = FixedPoint2.Zero;
        foreach (var organ in _part.GetPartOrgans(target.Owner).Values)
        {
            if (!TryComp<InternalOrganComponent>(organ, out var organComp))
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
                target.Comp.WoundableIntegrity / target.Comp.IntegrityCap / totalIntegrity
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
            target.Comp.ParentWoundable is not {} parent ||
            target.Comp.WoundableSeverity != WoundableSeverity.Mangled) // has to be mangled before possibly dismembering
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

        var bonePenalty = FixedPoint2.New(1); // higher means less chance to delimb
        if (TryComp<BonelessComponent>(target.Owner, out var bonelessComp))
            bonePenalty = bonelessComp.BonePenalty;

        // Healthy bones decrease the chance of your limb getting delimbed
        var multiplier = 1f;
        if (GetBone(target.AsNullable()) is {} bone)
        {
            switch (bone.Comp.BoneSeverity)
            {
                case BoneSeverity.Normal:
                    multiplier *= 0.3f; // decreases delimb chance by 70%
                    break;
                case BoneSeverity.Damaged:
                    multiplier *= 0.6f; // 40%
                    break;
                case BoneSeverity.Cracked:
                    multiplier *= 1f; // 0%
                    break;
                case BoneSeverity.Broken:
                    multiplier *= 1.2f; // increases by 20%
                    break;
                default:
                    break;
            }
        }

        // TODO SHITMED: this doesnt fucking work. also why is it using fixedpoint when 90% of it is floats fucking moron
        var chance =
            FixedPoint2.Clamp(
                (1f - (MathF.Pow(target.Comp.WoundableIntegrity.Float(), 1.3f) / target.Comp.IntegrityCap - 1f) * bonePenalty) * multiplier
                - deduction + woundInflicter.Comp.TraumasChances[TraumaType.Dismemberment],
                0,
                1);

        // TODO SHITMED: if above is fixed, predicted random
        var result = _random.Prob((float) chance);
        return result;
    }

    public EntityUid AddTrauma(
        EntityUid target,
        Entity<WoundableComponent> holdingWoundable,
        Entity<TraumaInflicterComponent> inflicter,
        TraumaType traumaType,
        FixedPoint2 severity,
        (BodyPartType, BodyPartSymmetry)? targetType = null)
    {
        if (TerminatingOrDeleted(inflicter))
            return EntityUid.Invalid;

        foreach (var trauma in inflicter.Comp.TraumaContainer.ContainedEntities)
        {
            var containedTraumaComp = Comp<TraumaComponent>(trauma);
            if (containedTraumaComp.TraumaType != traumaType
                || containedTraumaComp.TraumaTarget != target)
                continue;
            // Check for TraumaTarget isn't really necessary..
            // Right now wounds on a specified woundable can't wound other woundables, but in case IF something happens or IF someone decides to do that

            //  Allows us to create multiple dismemberment traumas on the same body part.
            if (targetType.HasValue
                && targetType.Value != containedTraumaComp.TargetType)
                continue;

            containedTraumaComp.TraumaSeverity = severity;
            return trauma;
        }

        var traumaEnt = Spawn(inflicter.Comp.TraumaPrototypes[traumaType]);
        var traumaComp = EnsureComp<TraumaComponent>(traumaEnt);

        traumaComp.TraumaSeverity = severity;

        traumaComp.TraumaTarget = target;

        if (targetType.HasValue)
            traumaComp.TargetType = targetType.Value;

        traumaComp.HoldingWoundable = holdingWoundable;

        _container.Insert(traumaEnt, inflicter.Comp.TraumaContainer);

        // Raise the event on the woundable
        var ev = new TraumaInducedEvent((traumaEnt, traumaComp), target, severity, traumaType);
        RaiseLocalEvent(holdingWoundable, ref ev);

        // Raise the event on the inflicter (wound)
        var ev1 = new TraumaInducedEvent((traumaEnt, traumaComp), target, severity, traumaType);
        RaiseLocalEvent(inflicter, ref ev1);

        Dirty(traumaEnt, traumaComp);
        return traumaEnt;
    }

    public void RemoveTrauma(
        Entity<TraumaComponent> trauma)
    {
        if (!_container.TryGetContainingContainer((trauma.Owner, Transform(trauma.Owner), MetaData(trauma.Owner)), out var traumaContainer))
            return;

        if (!TryComp<TraumaInflicterComponent>(traumaContainer.Owner, out var traumaInflicter))
            return;

        RemoveTrauma(trauma, (traumaContainer.Owner, traumaInflicter));
    }

    public void RemoveTrauma(
        Entity<TraumaComponent> trauma,
        Entity<TraumaInflicterComponent> inflicterWound)
    {
        _container.Remove(trauma.Owner, inflicterWound.Comp.TraumaContainer, reparent: false, force: true);

        if (trauma.Comp.TraumaTarget != null)
        {
            var ev = new TraumaBeingRemovedEvent(trauma, trauma.Comp.TraumaTarget.Value, trauma.Comp.TraumaSeverity, trauma.Comp.TraumaType);
            RaiseLocalEvent(inflicterWound, ref ev);

            if (trauma.Comp.HoldingWoundable != null)
            {
                var ev1 = new TraumaBeingRemovedEvent(trauma, trauma.Comp.TraumaTarget.Value, trauma.Comp.TraumaSeverity, trauma.Comp.TraumaType);
                RaiseLocalEvent(trauma.Comp.HoldingWoundable.Value, ref ev1);
            }
        }

        if (_net.IsServer)
            QueueDel(trauma);
    }

    #endregion

    #region Private API

    private void ApplyTraumas(Entity<WoundableComponent> target, Entity<TraumaInflicterComponent> inflicter, List<TraumaType> traumas, FixedPoint2 severity)
    {
        if (!TryComp<BodyPartComponent>(target, out var part) || _body.GetBody(target.Owner) is not {} body)
            return;

        _consciousness.TryGetNerveSystem(body, out var nerveSys);

        foreach (var trauma in traumas)
        {
            EntityUid? targetChosen = null;
            switch (trauma)
            {
                case TraumaType.BoneDamage:
                    targetChosen = target.Comp.Bone.ContainedEntities.FirstOrNull();
                    break;

                case TraumaType.OrganDamage:
                    var organs = new List<EntityUid>();
                    foreach (var organ in _part.GetPartOrgans((target.Owner, part)).Values)
                    {
                        if (HasComp<InternalOrganComponent>(organ))
                            organs.Add(organ);
                    }
                    // TODO SHITMED: predicted random
                    _random.Shuffle(organs);

                    if (organs.FirstOrNull() is {} chosenOrgan)
                        targetChosen = chosenOrgan;

                    break;
                case TraumaType.Dismemberment:
                    targetChosen = target.Comp.ParentWoundable;
                    break;

                case TraumaType.NerveDamage:
                    targetChosen = target;
                    break;
            }

            if (targetChosen == null)
                continue;

            var beforeTraumaInduced = new BeforeTraumaInducedEvent(severity, targetChosen.Value, trauma);
            RaiseLocalEvent(target, ref beforeTraumaInduced);

            if (beforeTraumaInduced.Cancelled)
                continue;

            switch (trauma)
            {
                case TraumaType.BoneDamage:
                    if (ApplyBoneTrauma(targetChosen.Value, target, inflicter, severity) && nerveSys is {} brain2)
                    {
                        _pain.TryAddPainModifier(
                            brain2,
                            target.Owner,
                            "BoneDamage",
                            severity / 1.4f,
                            PainDamageTypes.TraumaticPain,
                            brain2.Comp);
                    }

                    break;

                case TraumaType.OrganDamage:
                    var traumaEnt = AddTrauma(targetChosen.Value, target, inflicter, TraumaType.OrganDamage, severity);

                    if (traumaEnt != EntityUid.Invalid
                        && !TryChangeOrganDamageModifier(targetChosen.Value, severity, traumaEnt, "WoundableDamage"))
                    {
                        TryCreateOrganDamageModifier(targetChosen.Value, severity, traumaEnt, "WoundableDamage");
                    }

                    break;

                case TraumaType.NerveDamage:
                    if (nerveSys is not {} brain)
                        break;

                    var time = TimeSpan.FromSeconds((float) severity * 2.4);

                    // Fooling people into thinking they have no pain.
                    // 10 (raw pain) * 1.4 (multiplier) = 14 (actual pain)
                    // 1 - 0.28 = 0.72 (the fraction of pain the person feels)
                    // 14 * 0.72 = 10.08 (the pain the player can actually see) ... Barely noticeable :3
                    _pain.TryAddPainMultiplier(brain,
                        "NerveDamage",
                        1.4f,
                        time: time);

                    _pain.TryAddPainFeelsModifier(brain,
                        "NerveDamage",
                        target,
                        -0.28f,
                        time: time);
                    foreach (var child in _wound.GetAllWoundableChildren(target))
                    {
                        // Funner! Very unlucky of you if your torso gets hit. Rest in pieces
                        _pain.TryAddPainFeelsModifier(brain,
                            "NerveDamage",
                            child,
                            -0.7f,
                            time: time);
                    }

                    break;

                case TraumaType.Dismemberment:
                    if (!_wound.IsWoundableRoot(target)
                        && _wound.TryCreateWound(targetChosen.Value, Blunt, 0, out var woundCreated, Brute)) // We need this to add the trauma into.
                    {
                        AddTrauma(
                            targetChosen.Value,
                            (targetChosen.Value, Comp<WoundableComponent>(targetChosen.Value)),
                            (woundCreated.Value.Owner, EnsureComp<TraumaInflicterComponent>(woundCreated.Value.Owner)),
                            TraumaType.Dismemberment,
                            severity,
                            (part.PartType, part.Symmetry));

                        _wound.AmputateWoundable(targetChosen.Value, target, target);
                    }
                    break;
            }

            //Log.Debug($"A new trauma (Raw Severity: {severity}) was created on target: {ToPrettyString(target)}. Type: {trauma}.");
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
