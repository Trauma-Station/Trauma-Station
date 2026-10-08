// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Common.Medical;
using Content.Medical.Common.Body;
using Content.Medical.Common.CCVar;
using Content.Medical.Common.Damage;
using Content.Medical.Common.DoAfter;
using Content.Medical.Common.Healing;
using Content.Medical.Common.Targeting;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Medical.Shared.Wounds;

public sealed partial class WoundSystem
{
    [Dependency] private BodyStatusSystem _bodyStatus = default!;
    [Dependency] private GibbingSystem _gibbing = default!;

    private const string WoundContainerId = "Wounds";
    public static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    public static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    public static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";

    private readonly List<Entity<WoundComponent>> _wounds = new();

    #region Event Handling

    [SubscribeLocalEvent]
    private void OnWoundableInit(Entity<WoundableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Wounds = _container.EnsureContainer<Container>(ent, WoundContainerId);
    }

    [SubscribeLocalEvent]
    private void OnWoundRemoved(Entity<WoundComponent> wound, ref EntGotRemovedFromContainerMessage args)
    {
        if (wound.Comp.HoldingWoundable == EntityUid.Invalid || _timing.ApplyingState)
            return;

        wound.Comp.HoldingWoundable = EntityUid.Invalid;
        PredictedQueueDel(wound);
    }

    [SubscribeLocalEvent]
    private void OnWoundableInserted(Entity<WoundableComponent> parent, ref OrganInsertedIntoPartEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (_body.GetBody(parent.Owner) is {} body)
            _trauma.UpdateBodyBoneAlert(body);
    }

    [SubscribeLocalEvent]
    private void OnWoundableRemoved(Entity<WoundableComponent> parent, ref OrganRemovedFromPartEvent args)
    {
        if (_timing.ApplyingState ||
            !TryComp<WoundableComponent>(args.Organ, out var child))
            return;

        child.WoundableSeverity = WoundableSeverity.Severed;
        DirtyField(args.Organ, child, nameof(WoundableComponent.WoundableSeverity));

        if (_body.GetBody(parent.Owner) is {} body)
            _trauma.UpdateBodyBoneAlert(body);
    }

    [SubscribeLocalEvent]
    private void OnWoundableRemoveAttempt(Entity<WoundableComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        if (!ent.Comp.CanRemove)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void HealWoundsOnWoundableAttempt(Entity<WoundableComponent> woundable, ref WoundHealAttemptOnWoundableEvent args)
    {
        if (woundable.Comp.WoundableSeverity == WoundableSeverity.Severed)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnCheckPartWounded(Entity<WoundableComponent> ent, ref CheckPartWoundedEvent args)
    {
        foreach (var wound in GetWoundableWounds(ent.AsNullable()))
        {
            if (wound.Comp.WoundSeverity == WoundSeverity.Healed)
                continue;

            if (!args.DamageKeys.Contains(wound.Comp.DamageType))
                continue;

            args.Wounded = true;
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnCheckPartBleeding(Entity<WoundableComponent> ent, ref CheckPartBleedingEvent args)
    {
        foreach (var wound in GetWoundableWounds(ent.AsNullable()))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            args.Bleeding = true;
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnHealBleedingWounds(Entity<WoundableComponent> ent, ref HealBleedingWoundsEvent args)
    {
        TryHealBleedingWounds(ent.AsNullable(), args.BloodlossModifier, out var bleedStop);
        args.BleedStopAbility = bleedStop;
    }

    [SubscribeLocalEvent]
    private void OnWoundSeverityChanged(EntityUid wound, WoundComponent woundComponent, WoundSeverityChangedEvent args)
    {
        if (args.NewSeverity != WoundSeverity.Healed)
            return;

        RemoveWound(wound);
    }

    [SubscribeLocalEvent(after: new[] { typeof(DamageableSystem) })]
    private void OnDamageDealt(Entity<WoundableComponent> ent, ref DamageDealtEvent args)
    {
        if (!ent.Comp.AllowWounds)
            return;

        // Create or update wounds based on damage changes
        var part = ent.AsNullable();
        foreach (var (damageType, damageValue) in args.ModifiedDamage.DamageDict)
        {
            if (damageValue == 0)
                continue; // Only create wounds for damage or healing

            if (damageValue < 0)
            {
                TryHealWoundsOfType(part, -damageValue, damageType, out var healed, ignoreBlockers: args.IgnoreBlockers);
            }
            else
            {
                // Only create wound if it's a valid damage type for wounds
                var id = args.Damage.GetWoundId(damageType);
                if (!IsWoundPrototypeValid(id))
                    continue;

                var multiplier = args.Damage.WoundSeverityMultipliers.GetValueOrDefault(damageType, 1);
                TryInduceWound(part,
                    args.Damage.GetWoundId(damageType),
                    damageValue * multiplier,
                    out _,
                    damageType: damageType,
                    user: args.Origin);
            }
        }

        // Update woundable integrity based on new damage
        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);
    }

    [SubscribeLocalEvent]
    private void OnDamageSet(Entity<WoundableComponent> ent, ref DamageSetEvent args)
    {
        if (!ent.Comp.AllowWounds)
            return;

        var part = ent.AsNullable();

        // TODO: VERY sus
        var value = args.Damage;
        var damage = _damageable.GetAllDamage(ent.Owner);
        foreach (var type in damage.DamageDict.Keys)
        {
            var mul = damage.WoundSeverityMultipliers.GetValueOrDefault(type, 1);
            TryInduceWound(part, type.Id, value * mul, out _);
        }

        UpdateWoundableIntegrity(part);
    }

    [SubscribeLocalEvent]
    private void OnModifyDoAfterDelay(Entity<HandOrganComponent> ent, ref BodyRelayedEvent<ModifyDoAfterDelayEvent> args)
    {
        // TODO SHITMED: because of how the shitcode works, missing a hand is faster than having a broken one
        // make a thing like LegsComponent that makes doafters longer with missing hands
        RaiseLocalEvent(ent, args.Args);
    }

    #endregion

    #region Public API

    public ProtoId<DamageGroupPrototype>? GetDamageGroupByType([ForbidLiteral] ProtoId<DamageTypePrototype> id)
    {
        foreach (var group in ProtoMan.EnumeratePrototypes<DamageGroupPrototype>())
        {
            if (group.DamageTypes.Contains(id))
                return group.ID;
        }

        return null;
    }

    public bool TryInduceWound(
        Entity<WoundableComponent?> part,
        [ForbidLiteral] EntProtoId id,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundInduced,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        ProtoId<DamageTypePrototype>? damageType = null,
        EntityUid? user = null)
    {
        woundInduced = null;
        if (severity <= FixedPoint2.Zero || !_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        if (TryContinueWound(part, id, severity, out woundInduced, user))
            return true;

        damageGroup ??= GetDamageGroupByType(damageType ?? id.Id);

        return damageGroup != null && TryCreateWound(
            part,
            id,
            severity,
            out woundInduced,
            damageGroup.Value);
    }

    /// <summary>
    /// Opens a new wound on a requested woundable.
    /// </summary>
    /// <param name="part">The bodypart.</param>
    /// <param name="id">Wound prototype.</param>
    /// <param name="severity">Severity for wound to apply.</param>
    /// <param name="woundCreated">The wound that was created</param>
    /// <param name="damageGroup">Damage group.</param>
    public bool TryCreateWound(
        Entity<WoundableComponent?> part,
        [ForbidLiteral] EntProtoId id,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundCreated,
        ProtoId<DamageGroupPrototype>? damageGroup)
    {
        woundCreated = null;

        // allows 0 severity wounds for dismemberment traumas to exist
        if (severity < FixedPoint2.Zero ||
            TerminatingOrDeleted(part) ||
            !_timing.IsFirstTimePredicted ||
            !IsWoundPrototypeValid(id) ||
            !_woundableQuery.Resolve(part, ref part.Comp) ||
            !part.Comp.AllowWounds)
            return false;

        var wound = PredictedSpawnInContainerOrDrop(id, part, WoundContainerId);
        var comp = _query.Comp(wound);
        comp.HoldingWoundable = part.Owner;
        comp.DamageGroup = damageGroup;
        DirtyFields(wound, comp, null, nameof(WoundComponent.HoldingWoundable), nameof(WoundComponent.DamageGroup));

        SetWoundSeverity((wound, comp), severity);

        var ev = new WoundAddedEvent(comp, part.Comp);
        RaiseLocalEvent(wound, ref ev);

        woundCreated = (wound, comp);
        return true;
    }

    /// <summary>
    /// Continues wound with specific type, if there's any. Adds severity to it basically.
    /// </summary>
    /// <param name="part">Woundable bodypart</param>
    /// <param name="id">Wound entity's ID.</param>
    /// <param name="severity">Severity to apply.</param>
    /// <param name="woundContinued">The wound the severity was applied to, if any</param>
    /// <returns>Returns true, if wound was continued.</returns>
    public bool TryContinueWound(
        Entity<WoundableComponent?> part,
        [ForbidLiteral] EntProtoId id,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundContinued,
        EntityUid? user = null)
    {
        woundContinued = null;
        if (severity == FixedPoint2.Zero ||
            !IsWoundPrototypeValid(id))
            return false;

        foreach (var wound in GetWoundableWounds(part))
        {
            if (Prototype(wound)?.ID is not { } woundId ||
                id != woundId || wound.Comp.IsScar ||
                ChangeWoundSeverity(wound, severity, part.Comp, user) == FixedPoint2.Zero)
                continue;

            woundContinued = wound;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets severity of a wound, returning the change from the previous severity.
    /// </summary>
    /// <param name="wound">Wound to which severity is applied.</param>
    /// <param name="severity">Severity to set.</param>
    public FixedPoint2 SetWoundSeverity(Entity<WoundComponent> wound,
        FixedPoint2 severity,
        WoundableComponent? woundable = null,
        EntityUid? user = null)
    {
        var change = FixedPoint2.Zero;
        var part = wound.Comp.HoldingWoundable;
        if (!_woundableQuery.Resolve(wound.Comp.HoldingWoundable, ref woundable))
            return change;

        var old = wound.Comp.WoundSeverityPoint;

        var upperLimit = woundable.IntegrityCap;
        severity = FixedPoint2.Clamp(severity, 0, upperLimit);

        if (severity == old)
            return change;

        change = severity - old;
        wound.Comp.WoundSeverityPoint = severity;
        DirtyField(wound, wound.Comp, nameof(WoundComponent.WoundSeverityPoint));

        if (severity > old &&
            wound.Comp.MangleSeverity != null &&
            HasWoundsExceedingMangleSeverity(part))
        {
            _trauma.ApplyMangledTraumas((part, woundable), wound.Owner, severity, user);
        }

        WoundSeverityChanged(wound, old);

        CheckSeverityThresholds(wound, (part, woundable));

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);
        return change;
    }

    /// <summary>
    /// Increases a wound's severity by an amount.
    /// </summary>
    /// <param name="wound">Wound to which severity is applied.</param>
    /// <param name="severity">Severity to add.</param>
    public FixedPoint2 ChangeWoundSeverity(
        Entity<WoundComponent> wound,
        FixedPoint2 severity,
        WoundableComponent? woundable = null,
        EntityUid? user = null)
    {
        return SetWoundSeverity(wound, wound.Comp.WoundSeverityPoint + severity, woundable, user);
    }

    /// <summary>
    /// Amputates (not destroys) an entity's body part if conditions are met.
    /// </summary>
    /// <param name="parent">Parent of the woundable entity. Yes.</param>
    /// <param name="part">The vulnerable body part</param>
    public bool AmputateWoundable(Entity<WoundableComponent?> parent, Entity<WoundableComponent?> part, EntityUid? user = null)
    {
        if (_timing.ApplyingState ||
            !_woundableQuery.Resolve(parent, ref parent.Comp) ||
            !_woundableQuery.Resolve(part, ref part.Comp) ||
            _body.GetBody(parent) is not {} body ||
            _body.GetBody(part) != body || // the parts have to be from the same body
            !_body.RemoveOrgan(body, part.Owner))
            return false;

        _audio.PlayPredicted(part.Comp.WoundableDelimbedSound, body, user);

        var ampEv = new BeforeAmputationDamageEvent();
        RaiseLocalEvent(body, ref ampEv);

        if (!ampEv.Cancelled && part.Comp.DamageOnAmputate is {} damage)
            _damageable.ChangeDamage(parent.Owner, damage);

        if (parent.Comp.CanBleed)
        {
            foreach (var wound in GetWoundableWounds(parent))
            {
                if (!_bleedQuery.TryComp(wound, out var bleeds))
                    continue;

                bleeds.BleedingAmountRaw += 20f;
                bleeds.Scaling = 1f;
                bleeds.ScalingLimit = 1f;
                bleeds.IsBleeding = true;
                DirtyFields(wound, bleeds, null,
                    nameof(BleedInflicterComponent.BleedingAmountRaw),
                    nameof(BleedInflicterComponent.Scaling),
                    nameof(BleedInflicterComponent.ScalingLimit),
                    nameof(BleedInflicterComponent.IsBleeding));
            }
        }

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(part));
        var direction = rand.NextAngle().ToWorldVec();
        var dropAngle = rand.NextFloat(0.8f, 1.2f);
        var worldRotation = _transform.GetWorldRotation(part).ToVec();

        _throwing.TryThrow(
            part.Owner,
            rand.NextAngle().ToWorldVec() * rand.NextFloat(0.8f, 5f),
            rand.NextFloat(0.5f, 1f),
            pushbackRatio: 0.3f
        );

        return true;
    }

    #endregion

    #region Private API

    private void WoundSeverityChanged(Entity<WoundComponent> wound, FixedPoint2 old, FixedPoint2? overflow = null)
    {
        var total = wound.Comp.WoundSeverityPoint;
        var ev = new WoundSeverityPointChangedEvent(wound.Comp, old, total, overflow);
        RaiseLocalEvent(wound, ref ev);
    }

    /// <summary>
    /// Updates the woundable integrity based on the sum of its wounds.
    /// </summary>
    public void UpdateWoundableIntegrity(Entity<WoundableComponent?> part)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp) || part.Comp.Wounds == default) // it can be null while applying state if the entity is entering pvs right now
            return;

        // Calculate total damage on this part
        var damage = FixedPoint2.Zero;
        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            var woundComp = _query.Comp(wound);
            if (woundComp.IsScar) // scars don't affect limb integrity
                continue;

            damage += woundComp.WoundSeverityPoint;
        }

        var newIntegrity = FixedPoint2.Clamp(part.Comp.IntegrityCap - damage, 0, part.Comp.IntegrityCap);
        if (newIntegrity == part.Comp.Integrity)
            return;

        part.Comp.Integrity = newIntegrity;
        DirtyField(part, part.Comp, nameof(WoundableComponent.Integrity));
    }

    public bool AddWound(
        Entity<WoundableComponent?> part,
        Entity<WoundComponent?> wound,
        FixedPoint2 woundSeverity,
        ProtoId<DamageGroupPrototype>? damageGroup)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp) ||
            !_query.Resolve(wound, ref wound.Comp) ||
            !_timing.IsFirstTimePredicted ||
            part.Comp.Wounds.Contains(wound))
            return false;

        return _container.Insert(wound.Owner, part.Comp.Wounds);
    }

    private bool RemoveWound(EntityUid wound)
    {
        if (!_query.TryComp(wound, out var comp))
            return false;

        // We prevent removal if theres at least one wound holding traumas left.
        foreach (var trauma in _trauma.GetAllWoundTraumas(wound))
        {
            if (TraumaSystem.TraumasBlockingHealing.Contains(trauma.Comp.TraumaType))
                return false;
        }

        PredictedDel(wound);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnTraumaBeingRemoved(Entity<WoundComponent> ent, ref TraumaBeingRemovedEvent args)
    {
        if (ent.Comp.WoundSeverity == WoundSeverity.Healed)
            RemoveWound(ent); // Remove wound method will perform the check on if there are any other wounds pending treatment
    }

    [SubscribeLocalEvent]
    private void OnDecapitate(Entity<BodyComponent> ent, ref DecapitateEvent args)
    {
        if (!args.Handled &&
            _body.GetOrgan(ent, HeadCategory) is {} head &&
            _part.GetParentPart(head) is { } parent)
        {
            args.Handled = AmputateWoundable(parent, head, args.User);
        }
    }

    [SubscribeLocalEvent]
    private void OnCauterized(Entity<BodyComponent> ent, ref CauterizedEvent args)
    {
        TryHealMostSevereBleedingWoundables(ent, (float) args.Amount, out _, ent.Comp);
    }

    private void CheckSeverityThresholds(Entity<WoundComponent> wound,
        Entity<WoundableComponent?> part)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return;

        var nearestSeverity = wound.Comp.WoundSeverity;
        var scale = part.Comp.IntegrityCap / 100;
        foreach (var (severity, value) in _woundThresholds.OrderByDescending(kv => kv.Value))
        {
            var scaledThreshold = value * scale;
            if (wound.Comp.WoundSeverityPoint < scaledThreshold)
                continue;

            if (severity == WoundSeverity.Healed && wound.Comp.WoundSeverityPoint > 0)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity == wound.Comp.WoundSeverity)
            return;

        var ev = new WoundSeverityChangedEvent(wound.Comp.WoundSeverity, nearestSeverity);
        RaiseLocalEvent(wound, ref ev);

        wound.Comp.WoundSeverity = nearestSeverity;
        DirtyField(wound, wound.Comp, nameof(WoundComponent.WoundSeverity));
    }

    /// <summary>
    /// Checks if the current integrity crosses any severity thresholds and updates accordingly
    /// </summary>
    private void CheckWoundableSeverityThresholds(Entity<WoundableComponent?> part)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return;

        var nearestSeverity = part.Comp.WoundableSeverity;
        foreach (var (severity, value) in part.Comp.Thresholds.OrderByDescending(kv => kv.Value))
        {
            if (part.Comp.Integrity >= part.Comp.IntegrityCap)
            {
                nearestSeverity = WoundableSeverity.Healthy;
                break;
            }

            if (part.Comp.Integrity < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity == part.Comp.WoundableSeverity)
            return;

        part.Comp.WoundableSeverity = nearestSeverity;
        DirtyField(part, part.Comp, nameof(WoundableComponent.WoundableSeverity));

        if (_body.GetBody(part.Owner) is {} body)
            _bodyStatus.UpdateStatus(body);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Validates the wound prototype based on the given prototype ID.
    /// Checks if the specified prototype ID corresponds to a valid EntityPrototype in the collection,
    /// ensuring it contains the necessary WoundComponent.
    /// </summary>
    /// <param name="protoId">The prototype ID to be validated.</param>
    /// <returns>True if the wound prototype is valid, otherwise false.</returns>
    private bool IsWoundPrototypeValid([ForbidLiteral] EntProtoId id)
        => ProtoMan.TryIndex(id, out var proto)
            && proto.HasComp(_woundName);

    public Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity> GetWoundableStatesOnBody(EntityUid body)
    {
        var result = SeveredStates();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (_body.GetCategory(part.Owner) is {} category)
                result[category] = part.Comp.WoundableSeverity;
        }

        return result;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity> GetDamageableStatesOnBody(EntityUid body)
    {
        var result = SeveredStates();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (_body.GetCategory(part.Owner) is not {} category)
                continue;

            var nearestSeverity = WoundableSeverity.Severed;
            var damage = _damageable.GetTotalDamage(part.Owner);
            foreach (var (severity, threshold) in part.Comp.Thresholds.OrderByDescending(kv => kv.Value))
            {
                if (damage <= 0)
                {
                    nearestSeverity = WoundableSeverity.Healthy;
                    break;
                }

                if (damage >= part.Comp.IntegrityCap)
                {
                    nearestSeverity = WoundableSeverity.Mangled;
                    break;
                }

                if (damage > part.Comp.IntegrityCap - threshold)
                    continue;

                nearestSeverity = severity;
                break;
            }

            result[category] = nearestSeverity;
        }

        return result;
    }

    private static Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity> SeveredStates()
    {
        var result = new Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity>();
        foreach (var part in BodySystem.BodyParts)
        {
            result[part] = WoundableSeverity.Severed;
        }
        return result;
    }

    /// <summary>
    /// Get the wounds present on a specific woundable
    /// The returned list is reused between calls, do not store it
    /// </summary>
    /// <param name="targetEntity">Entity that owns the woundable</param>
    /// <param name="targetWoundable">Woundable component</param>
    /// <returns>An enumerable pointing to one of the found wounds</returns>
    public List<Entity<WoundComponent>> GetWoundableWounds(Entity<WoundableComponent?> part)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp) || part.Comp.Wounds == default) // it can be null while applying state if the entity is entering pvs right now
            return [];

        _wounds.Clear();
        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            _wounds.Add((wound, _query.Comp(wound)));
        }
        return _wounds;
    }

    /// <summary>
    /// Checks for wounds on an entity that have exceeded their MangleSeverity threshold
    /// </summary>
    public bool HasWoundsExceedingMangleSeverity(Entity<WoundableComponent?> part)
        => GetWoundableWounds(part)
            .Any(wound =>
                wound.Comp.MangleSeverity != null &&
                wound.Comp.WoundSeverity >= wound.Comp.MangleSeverity);

    /// <summary>
    /// Returns you the sum of all wounds on this woundable
    /// </summary>
    /// <param name="part">The woundable bodypart</param>
    /// <param name="damageGroup">The damage group of said wounds</param>
    /// <param name="healable">Are the wounds supposed to be healable</param>
    /// <returns>The severity sum</returns>
    public FixedPoint2 GetWoundableSeverityPoint(
        Entity<WoundableComponent?> part,
        string? damageGroup = null,
        bool healable = false,
        bool ignoreBlockers = false)
    {
        var wounds = GetWoundableWounds(part);

        if (damageGroup != null)
            wounds.RemoveAll(wound => wound.Comp.DamageGroup != damageGroup);

        if (healable)
            wounds.RemoveAll(wound => !CanHealWound(wound, ignoreBlockers));

        var sum = FixedPoint2.Zero;
        foreach (var wound in wounds)
        {
            sum += wound.Comp.WoundSeverityPoint;
        }

        return sum;
    }

    /// <summary>
    /// Returns you the integrity damage the woundable has
    /// </summary>
    /// <param name="targetEntity">The woundable uid</param>
    /// <param name="targetWoundable">The component</param>
    /// <param name="damageGroup">The damage group of wounds that induced the damage</param>
    /// <param name="healable">Is the integrity damage healable</param>
    /// <returns>The integrity damage</returns>
    public FixedPoint2 GetWoundableIntegrityDamage(
        Entity<WoundableComponent?> part,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        bool healable = false,
        bool ignoreBlockers = false)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp) ||
            part.Comp.Wounds.Count == 0)
            return FixedPoint2.Zero;

        var wounds = GetWoundableWounds(part);
        if (damageGroup != null)
            wounds.RemoveAll(wound => wound.Comp.DamageGroup != damageGroup);
        if (healable)
            wounds.RemoveAll(wound => !CanHealWound(wound, ignoreBlockers));

        return wounds.Aggregate(FixedPoint2.Zero, (current, wound) => current + wound.Comp.WoundSeverityPoint);
    }

    #endregion
}
