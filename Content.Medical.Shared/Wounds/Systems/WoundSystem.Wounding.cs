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
using Content.Medical.Common.Weapons;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Pain;
using Content.Medical.Shared.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Wounds;

public sealed partial class WoundSystem
{
    [Dependency] private readonly BodyStatusSystem _bodyStatus = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    private const string WoundContainerId = "Wounds";
    private const string BoneContainerId = "Bone";
    public static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    public static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    public static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";

    private void InitWounding()
    {
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableInit);
        SubscribeLocalEvent<WoundableComponent, MapInitEvent>(OnWoundableMapInit);
        SubscribeLocalEvent<WoundableComponent, OrganInsertedIntoPartEvent>(OnWoundableInserted);
        SubscribeLocalEvent<WoundableComponent, OrganRemovedFromPartEvent>(OnWoundableRemoved);
        SubscribeLocalEvent<WoundComponent, EntGotInsertedIntoContainerMessage>(OnWoundInserted);
        SubscribeLocalEvent<WoundComponent, EntGotRemovedFromContainerMessage>(OnWoundRemoved);
        SubscribeLocalEvent<WoundComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
        SubscribeLocalEvent<WoundableComponent, BeforeDamageChangedEvent>(DudeItsJustLikeMatrix);
        SubscribeLocalEvent<WoundableComponent, WoundHealAttemptOnWoundableEvent>(HealWoundsOnWoundableAttempt);
        SubscribeLocalEvent<WoundableComponent, CheckPartBleedingEvent>(OnCheckPartBleeding);
        SubscribeLocalEvent<WoundableComponent, HealBleedingWoundsEvent>(OnHealBleedingWounds);
        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<WoundableComponent, DamageSetEvent>(OnDamageSet);
        SubscribeLocalEvent<WoundableComponent, BodyRelayedEvent<ModifyDoAfterDelayEvent>>(OnModifyDoAfterDelay);
        SubscribeLocalEvent<WoundableComponent, AttemptHandsMeleeEvent>(OnAttemptHandsMelee);
        SubscribeLocalEvent<WoundableComponent, AttemptHandsShootEvent>(OnAttemptHandsShoot);
        SubscribeLocalEvent<TraumaInflicterComponent, TraumaBeingRemovedEvent>(OnTraumaBeingRemoved);

        SubscribeLocalEvent<BodyComponent, DecapitateEvent>(OnDecapitate);
        SubscribeLocalEvent<BodyComponent, CauterizedEvent>(OnCauterized);
    }

    #region Event Handling

    private void OnWoundableInit(EntityUid uid, WoundableComponent comp, ComponentInit args)
    {
        comp.RootWoundable = uid;
        comp.Wounds = _container.EnsureContainer<Container>(uid, WoundContainerId);
        comp.Bone = _container.EnsureContainer<Container>(uid, BoneContainerId);
    }

    private void OnWoundableMapInit(EntityUid uid, WoundableComponent comp, MapInitEvent args)
    {
        if (HasComp<BonelessComponent>(uid))
            return;

        var bone = Spawn(comp.BoneEntity);
        var boneComp = Comp<BoneComponent>(bone);
        _transform.SetParent(bone, uid);
        _container.Insert(bone, comp.Bone);
        boneComp.BoneWoundable = uid;
        Dirty(bone, boneComp);
    }

    private void OnWoundInserted(EntityUid uid, WoundComponent comp, EntGotInsertedIntoContainerMessage args)
    {
        if (comp.HoldingWoundable == EntityUid.Invalid)
            return;

        var parentWoundable = Comp<WoundableComponent>(comp.HoldingWoundable);

        if (!TryComp<WoundableComponent>(parentWoundable.RootWoundable, out var woundableRoot))
            return;

        var ev = new WoundAddedEvent(comp, parentWoundable, woundableRoot);
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(comp.HoldingWoundable, ref ev);

        if (_body.GetBody(comp.HoldingWoundable) is {} body)
        {
            var bodyEv = new WoundAddedOnBodyEvent((uid, comp), parentWoundable, woundableRoot);
            RaiseLocalEvent(body, ref bodyEv);
        }
    }

    private void OnWoundRemoved(Entity<WoundComponent> wound, ref EntGotRemovedFromContainerMessage args)
    {
        if (wound.Comp.HoldingWoundable == EntityUid.Invalid || _timing.ApplyingState)
            return;

        PredictedQueueDel(wound);

        if (!TryComp(wound.Comp.HoldingWoundable, out WoundableComponent? oldParentWoundable) ||
            !TryComp(oldParentWoundable.RootWoundable, out WoundableComponent? oldWoundableRoot))
            return;

        wound.Comp.HoldingWoundable = EntityUid.Invalid;

        var ev = new WoundRemovedEvent(wound, oldParentWoundable, oldWoundableRoot);
        RaiseLocalEvent(wound, ref ev);
    }

    private void OnWoundableInserted(Entity<WoundableComponent> parent, ref OrganInsertedIntoPartEvent args)
    {
        if (_timing.ApplyingState ||
            !TryComp<WoundableComponent>(args.Organ, out var child))
            return;

        InternalAddWoundableToParent(parent, args.Organ, parent.Comp, child);

        if (_body.GetBody(parent.Owner) is {} body)
            _trauma.UpdateBodyBoneAlert(body);
    }

    private void OnWoundableRemoved(Entity<WoundableComponent> parent, ref OrganRemovedFromPartEvent args)
    {
        if (_timing.ApplyingState ||
            !TryComp<WoundableComponent>(args.Organ, out var child))
            return;

        InternalRemoveWoundableFromParent(parent, args.Organ, parent.Comp, child);

        if (_body.GetBody(parent.Owner) is {} body)
            _trauma.UpdateBodyBoneAlert(body);
    }

    private void DudeItsJustLikeMatrix(EntityUid uid, WoundableComponent comp, ref BeforeDamageChangedEvent args)
    {
        return;
        /* The world is not ready for you yet...

        if (!args.CanBeCancelled
            || args.Damage.GetTotal() <= 0)
            return;

        var chance = comp.DodgeChance;

        var bodyPart = Comp<BodyPartComponent>(uid);
        if (args.Origin != null)
        {
            if (bodyPart.Body != null)
            {
                var bodyTransform = _transform.GetWorldPosition(bodyPart.Body.Value);
                var originTransform = _transform.GetWorldPosition(args.Origin.Value);

                var distance = (originTransform - bodyTransform).Length();
                if (distance < _cfg.GetCVar(SurgeryCVars.DodgeDistanceChance) * 2)
                {
                    chance = 0;
                }
                else
                {
                    var additionalChance =
                        distance
                        / _cfg.GetCVar(SurgeryCVars.DodgeDistanceChance)
                        * _cfg.GetCVar(SurgeryCVars.DodgeDistanceChange);
                    chance += additionalChance;
                }
            }
        }

        if (!_random.Prob(Math.Clamp((float) chance, 0, 1)))
            return;

        if (bodyPart.Body.HasValue)
        {
            // Critted or dead people of course can't dodge for shit.
            if (!_mobState.IsAlive(bodyPart.Body.Value))
                return;

            _popup.PopupEntity(Loc.GetString("woundable-dodged", ("entity", bodyPart.Body.Value)), bodyPart.Body.Value, PopupType.Medium);
        }

        args.Cancelled = true;*/
    }

    private void HealWoundsOnWoundableAttempt(Entity<WoundableComponent> woundable, ref WoundHealAttemptOnWoundableEvent args)
    {
        if (woundable.Comp.WoundableSeverity == WoundableSeverity.Severed)
            args.Cancelled = true;
    }

    private void OnCheckPartBleeding(Entity<WoundableComponent> ent, ref CheckPartBleedingEvent args)
    {
        foreach (var wound in GetWoundableWounds(ent, ent.Comp))
        {
            if (!TryComp<BleedInflicterComponent>(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            args.Bleeding = true;
            return;
        }
    }

    private void OnHealBleedingWounds(Entity<WoundableComponent> ent, ref HealBleedingWoundsEvent args)
    {
        TryHealBleedingWounds(ent, args.BloodlossModifier, out var bleedStop, ent.Comp);
        args.BleedStopAbility = bleedStop;
    }

    private void OnWoundSeverityChanged(EntityUid wound, WoundComponent woundComponent, WoundSeverityChangedEvent args)
    {
        if (args.NewSeverity != WoundSeverity.Healed)
            return;

        //TryMakeScar(wound, out _, woundComponent); // disabled as there is no way to heal scars currently?
        RemoveWound(wound, woundComponent);
    }

    private void OnDamageChanged(EntityUid uid, WoundableComponent component, ref DamageChangedEvent args)
    {
        // Skip if there was no damage delta or if wounds aren't allowed
        if (args.UncappedDamage == null // Goobstation
            || !component.AllowWounds
            || !_net.IsServer)
            return;

        // Create or update wounds based on damage changes
        foreach (var (damageType, damageValue) in args.UncappedDamage.DamageDict)
        {
            if (damageValue == 0)
                continue; // Only create wounds for damage or healing

            if (damageValue < 0)
            {
                TryHealWoundsOnWoundable(uid, -damageValue, damageType, out var healed, component, ignoreBlockers: args.IgnoreBlockers);
            }
            else
            {
                // Only create wound if it's a valid damage type for wounds
                if (!IsWoundPrototypeValid(damageType))
                    continue;

                TryInduceWound(uid,
                    damageType,
                    damageValue *
                    args.UncappedDamage.WoundSeverityMultipliers.GetValueOrDefault(damageType, 1),
                    out _,
                    component);
            }
        }

        // Update woundable integrity based on new damage
        UpdateWoundableIntegrity(uid, component);
        CheckWoundableSeverityThresholds(uid, component);
    }

    private void OnDamageSet(Entity<WoundableComponent> ent, ref DamageSetEvent args)
    {
        if (!ent.Comp.AllowWounds)
            return;

        UpdateWoundableIntegrity(ent, ent.Comp);

        if (!TryComp<DamageableComponent>(ent, out var damage))
            return;

        var value = args.Damage;
        foreach (var type in damage.Damage.DamageDict.Keys)
        {
            var mul = damage.Damage.WoundSeverityMultipliers.GetValueOrDefault(type, 1);
            TryInduceWound(ent, type, value * mul, out _, ent.Comp);
        }
    }

    private void OnModifyDoAfterDelay(Entity<WoundableComponent> ent, ref BodyRelayedEvent<ModifyDoAfterDelayEvent> args)
    {
        var integrity = ent.Comp.WoundableIntegrity;
        // random hardcoded number award
        if (integrity > 50)
            return;

        args.Args.Multiplier *= (float) (integrity / ent.Comp.IntegrityCap);
    }

    private void OnAttemptHandsMelee(EntityUid uid, WoundableComponent component, ref AttemptHandsMeleeEvent args)
    {
        if (component.WoundableIntegrity > 25
            || args.Cancelled
            || _body.GetBody(uid) is not {} body)
            return;

        if (TryFumble("arm-fumble", new SoundPathSpecifier("/Audio/Effects/slip.ogg"), body, 0.20f))
            args.Cancelled = true;
    }

    private void OnAttemptHandsShoot(EntityUid uid, WoundableComponent component, ref AttemptHandsShootEvent args)
    {
        if (component.WoundableIntegrity > 25
            || args.Cancelled
            || _body.GetBody(uid) is not {} body)
            return;

        if (TryFumble("arm-fumble", new SoundPathSpecifier("/Audio/Effects/slip.ogg"), body, 0.20f))
            args.Cancelled = true;
    }

    #endregion

    #region Public API

    public DamageGroupPrototype? GetDamageGroupByType(string id)
    {
        return (from @group in _prototype.EnumeratePrototypes<DamageGroupPrototype>()
                where @group.DamageTypes.Contains(id)
                select @group).FirstOrDefault();
    }

    public bool TryInduceWounds(
        EntityUid uid,
        DamageSpecifier damage,
        out List<Entity<WoundComponent>> woundsInduced,
        WoundableComponent? woundable = null)
    {
        woundsInduced = new List<Entity<WoundComponent>>();
        if (!Resolve(uid, ref woundable))
            return false;

        foreach (var woundToInduce in damage.DamageDict)
        {
            if (!TryInduceWound(uid, woundToInduce.Key, woundToInduce.Value *
                damage.WoundSeverityMultipliers.GetValueOrDefault(woundToInduce.Key, 1), out var woundInduced, woundable))
                return false;

            woundsInduced.Add(woundInduced.Value);
        }

        return true;
    }

    public bool TryInduceWound(
        EntityUid uid,
        string woundId,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundInduced,
        WoundableComponent? woundable = null)
    {
        woundInduced = null;
        if (severity == FixedPoint2.Zero || !Resolve(uid, ref woundable))
            return false;

        if (TryContinueWound(uid, woundId, severity, out woundInduced, woundable))
            return true;

        var wound = TryCreateWound(
            uid,
            woundId,
            severity,
            out woundInduced,
            (from @group in _prototype.EnumeratePrototypes<DamageGroupPrototype>()
                where @group.DamageTypes.Contains(woundId)
                select @group).FirstOrDefault()
                ?.ID,
            woundable);
        return wound;
    }

    /// <summary>
    /// Opens a new wound on a requested woundable.
    /// </summary>
    /// <param name="uid">UID of the woundable (body part).</param>
    /// <param name="woundProtoId">Wound prototype.</param>
    /// <param name="severity">Severity for wound to apply.</param>
    /// <param name="woundCreated">The wound that was created</param>
    /// <param name="damageGroup">Damage group.</param>
    /// <param name="woundable">Woundable component.</param>
    public bool TryCreateWound(
        EntityUid uid,
        string woundProtoId,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundCreated,
        ProtoId<DamageGroupPrototype>? damageGroup,
        WoundableComponent? woundable = null)
    {
        woundCreated = null;

        if (!IsWoundPrototypeValid(woundProtoId)
            || !Resolve(uid, ref woundable))
            return false;

        var wound = Spawn(woundProtoId);
        if (AddWound(uid, wound, severity, damageGroup))
        {
            woundCreated = (wound, _query.Comp(wound));
        }
        else
        {
            // The wound failed some important checks, and we cannot let an invalid wound to be spawned!
            if (_net.IsServer && !IsClientSide(wound))
                QueueDel(wound);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Continues wound with specific type, if there's any. Adds severity to it basically.
    /// </summary>
    /// <param name="uid">Woundable entity's UID.</param>
    /// <param name="id">Wound entity's ID.</param>
    /// <param name="severity">Severity to apply.</param>
    /// <param name="woundContinued">The wound the severity was applied to, if any</param>
    /// <param name="woundable">Woundable for wound to add.</param>
    /// <returns>Returns true, if wound was continued.</returns>
    public bool TryContinueWound(
        EntityUid uid,
        string id,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundContinued,
        WoundableComponent? woundable = null)
    {
        woundContinued = null;
        if (severity == FixedPoint2.Zero ||
            !IsWoundPrototypeValid(id) ||
            !Resolve(uid, ref woundable))
            return false;

        var proto = _prototype.Index(id);
        foreach (var wound in GetWoundableWounds(uid, woundable))
        {
            if (proto.ID != wound.Comp.DamageType
                || wound.Comp.IsScar)
                continue;

            ApplyWoundSeverity(wound, severity, wound);
            woundContinued = wound;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to create a scar on a woundable entity. Takes a scar prototype from WoundComponent.
    /// </summary>
    /// <param name="wound">The wound entity, from which the scar will be made.</param>
    /// <param name="scarWound">The result scar wound, if created.</param>
    /// <param name="woundComponent">The WoundComponent representing a specific wound.</param>
    public bool TryMakeScar(EntityUid wound,
        [NotNullWhen(true)] out Entity<WoundComponent>? scarWound,
        WoundComponent? woundComponent = null)
    {
        scarWound = null;
        if (!Resolve(wound, ref woundComponent))
            return false;

        if (!_random.Prob(_cfg.GetCVar(SurgeryCVars.WoundScarChance)))
            return false;

        if (woundComponent.ScarWound == null || woundComponent.IsScar)
            return false;

        if (!TryCreateWound(woundComponent.HoldingWoundable,
                woundComponent.ScarWound,
                0.1f,
                out var createdWound,
                woundComponent.DamageGroup))
            return false;

        scarWound = createdWound;
        return true;
    }

    /// <summary>
    /// Sets severity of a wound.
    /// </summary>
    /// <param name="uid">UID of the wound.</param>
    /// <param name="severity">Severity to set.</param>
    /// <param name="wound">Wound to which severity is applied.</param>
    public void SetWoundSeverity(EntityUid uid,
        FixedPoint2 severity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(uid, ref wound)
            || !Resolve(wound.HoldingWoundable, ref woundable))
            return;

        var old = wound.WoundSeverityPoint;

        var upperLimit = wound.WoundSeverityPoint + woundable.WoundableIntegrity;
        wound.WoundSeverityPoint =
        FixedPoint2.Clamp(ApplySeverityModifiers(wound.HoldingWoundable, severity), 0, upperLimit);

        if (wound.WoundSeverityPoint != old)
            WoundSeverityChanged((uid, wound), old);

        CheckSeverityThresholds(uid, wound.HoldingWoundable, wound, woundable);
        Dirty(uid, wound);

        UpdateWoundableIntegrity(wound.HoldingWoundable);
        CheckWoundableSeverityThresholds(wound.HoldingWoundable);
    }

    /// <summary>
    /// Applies severity to a wound
    /// </summary>
    /// <param name="uid">UID of the wound.</param>
    /// <param name="severity">Severity to add.</param>
    /// <param name="wound">Wound to which severity is applied.</param>
    /// <param name="traumaList">Traumas to apply when applying severity.. Please use _trauma.RandomTraumaChance if you expect your thing to apply traumas.</param>
    public void ApplyWoundSeverity(
        EntityUid uid,
        FixedPoint2 severity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(uid, ref wound)
            || !Resolve(wound.HoldingWoundable, ref woundable))
            return;

        var old = wound.WoundSeverityPoint;
        var rawValue = severity > 0
            ? old + ApplySeverityModifiers(wound.HoldingWoundable, severity)
            : old + severity;

        var upperLimit = wound.WoundSeverityPoint + woundable.WoundableIntegrity;
        wound.WoundSeverityPoint = FixedPoint2.Clamp(rawValue, 0, upperLimit);
        Dirty(uid, wound);
        if (wound.WoundSeverityPoint != old || rawValue > wound.WoundSeverityPoint)
        {
            // We keep track of this overflow variable to allow continuous damage on wounds that have been capped
            // i.e. slashing nonstop at a dead body to continue inflicting traumas.
            FixedPoint2? overflow = rawValue > wound.WoundSeverityPoint ? rawValue - wound.WoundSeverityPoint : null;
            WoundSeverityChanged((uid, wound), old, overflow);
        }

        if (severity > 0
            && wound.MangleSeverity != null
            && HasWoundsExceedingMangleSeverity(wound.HoldingWoundable))
            _trauma.ApplyMangledTraumas(wound.HoldingWoundable, uid, severity, woundable);

        var holdingWoundable = wound.HoldingWoundable;
        CheckSeverityThresholds(uid, holdingWoundable, wound, woundable);

        UpdateWoundableIntegrity(holdingWoundable);
        CheckWoundableSeverityThresholds(holdingWoundable);
    }

    public FixedPoint2 ApplySeverityModifiers(
        EntityUid woundable,
        FixedPoint2 severity,
        WoundableComponent? component = null)
    {
        if (!Resolve(woundable, ref component))
            return severity;

        if (component.SeverityMultipliers.Count == 0)
            return severity;

        var toMultiply =
            component.SeverityMultipliers.Sum(multiplier => (float) multiplier.Value.Change) / component.SeverityMultipliers.Count;
        return severity * toMultiply;
    }

    /// <summary>
    /// Applies severity multiplier to a wound.
    /// </summary>
    /// <param name="uid">UID of the woundable.</param>
    /// <param name="owner">UID of the multiplier owner.</param>
    /// <param name="change">The severity multiplier itself</param>
    /// <param name="identifier">A string to defy this multiplier from others.</param>
    /// <param name="component">Woundable to which severity multiplier is applied.</param>
    public bool TryAddWoundableSeverityMultiplier(
        EntityUid uid,
        EntityUid owner,
        FixedPoint2 change,
        string identifier,
        WoundableComponent? component = null)
    {
        if (!Resolve(uid, ref component)
            || component.Wounds == null
            || !_net.IsServer)
            return false;

        if (!component.SeverityMultipliers.TryAdd(owner, new WoundableSeverityMultiplier(change, identifier)))
            return false;

        foreach (var wound in component.Wounds.ContainedEntities)
            CheckSeverityThresholds(wound, uid, woundableComp: component);

        UpdateWoundableIntegrity(uid, component);
        CheckWoundableSeverityThresholds(uid, component);

        return true;
    }

    /// <summary>
    /// Removes a multiplier from a woundable.
    /// </summary>
    /// <param name="uid">UID of the woundable.</param>
    /// <param name="identifier">Identifier of the said multiplier.</param>
    /// <param name="component">Woundable to which severity multiplier is applied.</param>
    public bool TryRemoveWoundableSeverityMultiplier(
        EntityUid uid,
        string identifier,
        WoundableComponent? component = null)
    {
        if (!Resolve(uid, ref component)
            || component.Wounds == null
            || !_net.IsServer)
            return false;

        foreach (var multiplier in component.SeverityMultipliers.Where(multiplier => multiplier.Value.Identifier == identifier))
        {
            if (!component.SeverityMultipliers.Remove(multiplier.Key, out _))
                return false;

            foreach (var wound in component.Wounds.ContainedEntities)
                CheckSeverityThresholds(wound, uid, woundableComp: component);

            UpdateWoundableIntegrity(uid, component);
            CheckWoundableSeverityThresholds(uid, component);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Changes a multiplier's change in a specified woundable.
    /// </summary>
    /// <param name="uid">UID of the woundable.</param>
    /// <param name="identifier">Identifier of the said multiplier.</param>
    /// <param name="change">The new multiplier fixed point.</param>
    /// <param name="component">Woundable to which severity multiplier is applied.</param>
    public bool TryChangeWoundableSeverityMultiplier(
        EntityUid uid,
        string identifier,
        FixedPoint2 change,
        WoundableComponent? component = null)
    {
        if (!Resolve(uid, ref component)
            || component.Wounds == null
            || !_net.IsServer)
            return false;

        foreach (var multiplier in component.SeverityMultipliers.Where(multiplier => multiplier.Value.Identifier == identifier))
        {
            component.SeverityMultipliers.Remove(multiplier.Key, out var value);

            value.Change = change;
            component.SeverityMultipliers.Add(multiplier.Key, value);

            foreach (var wound in component.Wounds.ContainedEntities.ToList())
                CheckSeverityThresholds(wound, uid, woundableComp: component);

            UpdateWoundableIntegrity(uid, component);
            CheckWoundableSeverityThresholds(uid, component);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Destroys an entity's body part if conditions are met.
    /// </summary>
    /// <param name="parentWoundableEntity">Parent of the woundable entity. Yes.</param>
    /// <param name="woundableEntity">The entity containing the vulnerable body part</param>
    /// <param name="woundableComp">Woundable component of woundableEntity.</param>
    public void DestroyWoundable(EntityUid parentWoundableEntity, EntityUid woundableEntity, WoundableComponent woundableComp)
    {
        if (!TryComp<BodyPartComponent>(woundableEntity, out var part))
            return;

        if (_body.GetBody(woundableEntity) is not {} body)
        {
            DropWoundableOrgans(woundableEntity, woundableComp);
            PredictedQueueDel(woundableEntity);
            return;
        }

        // if wounds amount somehow changes it triggers an enumeration error. owch
        woundableComp.WoundableSeverity = WoundableSeverity.Severed;

        _bodyStatus.UpdateStatus(body);

        // TODO SHITMED: if predicting this add user to pass to this
        if (_net.IsServer)
            _audio.PlayPvs(woundableComp.WoundableDestroyedSound, body);
        _appearance.SetData(woundableEntity,
            WoundableVisualizerKeys.Wounds,
            new WoundVisualizerGroupData(GetWoundableWounds(woundableEntity).Select(ent => GetNetEntity(ent)).ToList()));

        // add a dismemberment trauma to the parent part
        // this will prevent reattachment until it is cleaned up
        if (TryCreateWound(parentWoundableEntity, Blunt, 0f, out var woundCreated, Brute))
        {
            var traumaInflicter = EnsureComp<TraumaInflicterComponent>(woundCreated.Value.Owner);

            _trauma.AddTrauma(
                parentWoundableEntity,
                (parentWoundableEntity, Comp<WoundableComponent>(parentWoundableEntity)),
                (woundCreated.Value.Owner, traumaInflicter),
                TraumaType.Dismemberment,
                15f,
                (part.PartType, part.Symmetry));

            var bleedInflicter = EnsureComp<BleedInflicterComponent>(woundCreated.Value.Owner);
            bleedInflicter.BleedingAmountRaw += 20f;
            bleedInflicter.Scaling = 1f;
            bleedInflicter.ScalingLimit = 1f;
            bleedInflicter.IsBleeding = true;
            Dirty(woundCreated.Value.Owner, bleedInflicter);
        }

        Dirty(woundableEntity, woundableComp);

        // gibbing the body
        if (IsWoundableRoot(woundableEntity, woundableComp))
        {
            DropWoundableOrgans(woundableEntity, woundableComp);
            DestroyWoundableChildren(woundableEntity, woundableComp);
            _gibbing.Gib(body);

            PredictedQueueDel(woundableEntity);
            return;
        }

        foreach (var wound in GetWoundableWounds(woundableEntity, woundableComp))
            TransferWoundDamage(parentWoundableEntity, woundableEntity, wound, body);

        _body.RemoveOrgan(body, woundableEntity);

        // drop the organs and destroy the part
        _gibbing.Gib(woundableEntity);
    }

    /// <summary>
    /// Amputates (not destroys) an entity's body part if conditions are met.
    /// </summary>
    /// <param name="parentWoundableEntity">Parent of the woundable entity. Yes.</param>
    /// <param name="woundableEntity">The entity containing the vulnerable body part</param>
    /// <param name="woundableComp">Woundable component of woundableEntity.</param>
    public bool AmputateWoundable(EntityUid parentWoundableEntity, EntityUid woundableEntity, WoundableComponent? woundableComp = null, EntityUid? user = null)
    {
        if (_timing.ApplyingState ||
            !Resolve(woundableEntity, ref woundableComp) ||
            !woundableComp.CanRemove ||
            _body.GetBody(parentWoundableEntity) is not {} body)
            return false;

        // TODO SHITMED: why isnt this codepath predicted
        _audio.PlayPredicted(woundableComp.WoundableDelimbedSound, body, user);

        var ampEv = new BeforeAmputationDamageEvent();
        RaiseLocalEvent(body, ref ampEv);

        if (!ampEv.Cancelled && woundableComp.DamageOnAmputate is {} damage)
            _damageable.ChangeDamage(parentWoundableEntity, damage);

        AmputateWoundableSafely(parentWoundableEntity, woundableEntity);

        foreach (var wound in GetWoundableWounds(woundableEntity, woundableComp))
            TransferWoundDamage(parentWoundableEntity, woundableEntity, wound, body);

        foreach (var wound in GetWoundableWounds(parentWoundableEntity))
        {
            if (!TryComp<BleedInflicterComponent>(wound, out var bleeds)
                || !TryComp<WoundableComponent>(parentWoundableEntity, out var parentWoundable)
                || !parentWoundable.CanBleed)
                continue;

            // Goobstation start
            bleeds.BleedingAmountRaw += 20f;
            bleeds.Scaling = 1f;
            bleeds.ScalingLimit = 1f;
            bleeds.IsBleeding = true;
            //bleeds.ScalingLimit += 6;
            // Goobstation end
        }

        // TODO SHITMED: predict this...
        if (!_net.IsServer)
            return true;

        var direction = _random.NextAngle().ToWorldVec();
        var dropAngle = _random.NextFloat(0.8f, 1.2f);
        var worldRotation = _transform.GetWorldRotation(woundableEntity).ToVec();

        _throwing.TryThrow(
            woundableEntity,
            _random.NextAngle().ToWorldVec() * _random.NextFloat(0.8f, 5f),
            _random.NextFloat(0.5f, 1f),
            pushbackRatio: 0.3f,
            predicted: false // TODO SHITMED
        );

        return true;
    }

    /// <summary>
    /// Does whatever AmputateWoundable does, but does it without pain and the other mess.
    /// </summary>
    /// <param name="parentWoundableEntity">Parent of the woundable entity. Yes.</param>
    /// <param name="woundableEntity">The entity containing the vulnerable body part</param>
    /// <param name="woundableComp">Woundable component of woundableEntity.</param>
    public void AmputateWoundableSafely(EntityUid parentWoundableEntity,
        EntityUid woundableEntity,
        WoundableComponent? woundableComp = null)
    {
        if (!Resolve(woundableEntity, ref woundableComp)
            || !woundableComp.CanRemove)
            return;

        if (_body.GetBody(parentWoundableEntity) is not {} body)
            return;

        woundableComp.WoundableSeverity = WoundableSeverity.Severed;
        Dirty(woundableEntity, woundableComp);

        _bodyStatus.UpdateStatus(body);

        _appearance.SetData(woundableEntity,
            WoundableVisualizerKeys.Wounds,
            new WoundVisualizerGroupData(GetWoundableWounds(woundableEntity).Select(ent => GetNetEntity(ent)).ToList()));

        // Still does the funny popping, if the children are critted. for the funny :3
        DestroyWoundableChildren(woundableEntity, woundableComp, amputateChildrenSafely: true);
        _body.RemoveOrgan(body, woundableEntity);
    }

    #endregion

    #region Private API

    private void WoundSeverityChanged(Entity<WoundComponent> wound, FixedPoint2 old, FixedPoint2? overflow = null)
    {
        var total = wound.Comp.WoundSeverityPoint;
        var ev = new WoundSeverityPointChangedEvent(wound.Comp, old, total, overflow);
        RaiseLocalEvent(wound, ref ev);

        if (_body.GetBody(wound.Comp.HoldingWoundable) is not {} body)
            return;

        var bodySeverity = GetTotalWoundSeverity(body);
        var bodyEv = new WoundSeverityPointChangedOnBodyEvent(
            wound,
            bodySeverity - (total - old),
            bodySeverity);
        RaiseLocalEvent(body, ref ev);
    }

    private void DropWoundableOrgans(EntityUid woundable, WoundableComponent? woundableComp)
    {
        if (!Resolve(woundable, ref woundableComp, false) || !TryComp<BodyPartComponent>(woundable, out var part))
            return;

        foreach (var organ in _part.GetPartOrgans((woundable, part)).Values)
        {
            if (!TryComp<InternalOrganComponent>(organ, out var organComp))
                continue;

            if (organComp.OrganSeverity == OrganSeverity.Normal)
            {
                // TODO: SFX for organs getting not destroyed, but thrown out
                _part.RemoveOrgan((woundable, part), organ.AsNullable());
                var direction = _random.NextAngle().ToWorldVec();
                var dropAngle = _random.NextFloat(0.8f, 1.2f);
                var worldRotation = _transform.GetWorldRotation(organ).ToVec();

                _throwing.TryThrow(
                    organ,
                    _random.NextAngle().RotateVec(direction / dropAngle + worldRotation / 50),
                    0.5f * dropAngle * _random.NextFloat(-0.9f, 1.1f),
                    doSpin: false,
                    pushbackRatio: 0
                );
            }
            else
            {
                // Destroy it
                _trauma.TrySetOrganDamageModifier(
                    organ,
                    organComp.OrganIntegrity * 100,
                    woundable,
                    "LETMETELLYOUHOWMUCHIVECOMETOHATEYOUSINCEIBEGANTOLIVE",
                    organComp);
            }
        }
    }

    private void TransferWoundDamage(
        EntityUid parent,
        EntityUid severed,
        EntityUid wound,
        EntityUid body,
        WoundableComponent? woundableComp = null,
        WoundComponent? woundComp = null,
        BodyComponent? bodyComp = null)
    {
        // Goobstation start - commented out
        /*if (!Resolve(parent, ref woundableComp, false)
            || !Resolve(wound, ref woundComp, false)
            || !Resolve(body, ref bodyComp, false)
            || !_prototype.TryIndex(woundComp.DamageType, out DamageTypePrototype? damageType))
            return;

        var bodyPart = Comp<BodyPartComponent>(severed);

        if (TryComp(severed, out DamageableComponent? severedDamageable)
            && bodyComp.RootContainer.ContainedEntities.Count > 0
            && severedDamageable.Damage.DamageDict.TryGetValue(woundComp.DamageType, out var damage))
        {
            _damageable.TryChangeDamage(bodyComp.RootContainer.ContainedEntities.First(),
                new DamageSpecifier(damageType, damage),
                ignoreResistances: true,
                interruptsDoAfters: false);
        }

        foreach (var woundEnt in GetWoundableWounds(parent, woundableComp))
        {
            if (woundEnt.Comp.DamageType != woundComp.DamageType)
                continue;

            var tourniquetable = EnsureComp<TourniquetableComponent>(woundEnt);
            tourniquetable.SeveredSymmetry = bodyPart.Symmetry;
            tourniquetable.SeveredPartType = bodyPart.PartType;
        }*/
        // Goobstation end
    }

    /// <summary>
    /// Updates the woundable integrity based on the current damage
    /// </summary>
    public void UpdateWoundableIntegrity(EntityUid uid, WoundableComponent? component = null, DamageableComponent? damageable = null)
    {
        if (!Resolve(uid, ref component, false)
            || !Resolve(uid, ref damageable, false)
            || component.Wounds is not {} container)
            return;

        // Calculate total damage on this part
        var damage = FixedPoint2.Zero;
        foreach (var wound in container.ContainedEntities)
        {
            var woundComp = _query.Comp(wound);
            if (woundComp.IsScar) // scars don't affect limb integrity
                continue;

            damage += woundComp.WoundSeverityPoint;
        }

        var newIntegrity = FixedPoint2.Clamp(component.IntegrityCap - damage, 0, component.IntegrityCap);
        if (newIntegrity == component.WoundableIntegrity)
            return;

        var ev = new WoundableIntegrityChangedEvent(component.WoundableIntegrity, newIntegrity);
        RaiseLocalEvent(uid, ref ev);

        component.WoundableIntegrity = newIntegrity;
        Dirty(uid, component);
    }

    public bool AddWound( // Trauma - made public
        EntityUid target,
        EntityUid wound,
        FixedPoint2 woundSeverity,
        ProtoId<DamageGroupPrototype>? damageGroup,
        WoundableComponent? woundableComponent = null,
        WoundComponent? woundComponent = null)
    {
        if (!_net.IsServer
            || !Resolve(target, ref woundableComponent)
            || !Resolve(wound, ref woundComponent)
            || woundableComponent.Wounds == null
            || woundableComponent.Wounds.Contains(wound)
            || !_timing.IsFirstTimePredicted
            || !woundableComponent.AllowWounds)
            return false;

        _transform.SetParent(wound, target);
        woundComponent.HoldingWoundable = target;
        woundComponent.DamageGroup = damageGroup;

        if (!_container.Insert(wound, woundableComponent.Wounds))
            return false;

        SetWoundSeverity(wound, woundSeverity, woundComponent);
        var woundMeta = MetaData(wound);
        var targetMeta = MetaData(target);

        //Log.Debug($"Wound: {woundMeta.EntityPrototype!.ID}({wound}) created on {targetMeta.EntityPrototype!.ID}({target})");

        Dirty(wound, woundComponent);
        Dirty(target, woundableComponent);

        return true;
    }

    private bool RemoveWound(EntityUid woundEntity, WoundComponent? wound = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        if (!Resolve(woundEntity, ref wound, false)
            || !TryComp(wound.HoldingWoundable, out WoundableComponent? woundable))
            return false;

        //Log.Debug($"Wound: {MetaData(woundEntity).EntityPrototype!.ID}({woundEntity}) removed on {MetaData(wound.HoldingWoundable).EntityPrototype!.ID}({wound.HoldingWoundable})");

        UpdateWoundableIntegrity(wound.HoldingWoundable, woundable);
        CheckWoundableSeverityThresholds(wound.HoldingWoundable, woundable);

        // We prevent removal if theres at least one wound holding traumas left.
        foreach (var trauma in _trauma.GetAllWoundTraumas(woundEntity))
            if (TraumaSystem.TraumasBlockingHealing.Contains(trauma.Comp.TraumaType))
                return false;

        _container.Remove(woundEntity, woundable.Wounds!, false, true);

        return true;
    }

    private void OnTraumaBeingRemoved(Entity<TraumaInflicterComponent> ent, ref TraumaBeingRemovedEvent args)
    {
        if (_query.TryComp(ent, out var woundComp) &&
            woundComp.WoundSeverity == WoundSeverity.Healed)
        {
            RemoveWound(ent); // Remove wound method will perform the check on if there are any other wounds pending treatment
        }
    }

    private void OnDecapitate(Entity<BodyComponent> ent, ref DecapitateEvent args)
    {
        if (!args.Handled
            && _body.GetOrgan(ent, HeadCategory) is {} head
            && TryComp<WoundableComponent>(head, out var woundable)
            && woundable.ParentWoundable is {} parent)
            args.Handled = AmputateWoundable(parent, head, woundable, args.User);
    }

    private void OnCauterized(Entity<BodyComponent> ent, ref CauterizedEvent args)
    {
        TryHealMostSevereBleedingWoundables(ent, (float) args.Amount, out _, ent.Comp);
    }

    private void InternalAddWoundableToParent(
        EntityUid parentEntity,
        EntityUid childEntity,
        WoundableComponent parentWoundable,
        WoundableComponent childWoundable)
    {
        parentWoundable.ChildWoundables.Add(childEntity);
        childWoundable.ParentWoundable = parentEntity;
        childWoundable.RootWoundable = parentWoundable.RootWoundable;

        FixWoundableRoots(childEntity, childWoundable);

        if (!TryComp<WoundableComponent>(parentWoundable.RootWoundable, out var woundableRoot))
            return;

        var body = _body.GetBody(childEntity);
        var woundableAttached = new WoundableAttachedEvent(parentEntity, parentWoundable);
        RaiseLocalEvent(childEntity, ref woundableAttached);

        foreach (var (woundId, wound) in GetAllWounds(childEntity, childWoundable))
        {
            var ev = new WoundAddedEvent(wound, parentWoundable, woundableRoot);
            RaiseLocalEvent(woundId, ref ev);

            if (body is {} bodyUid)
            {
                var bodyEv = new WoundAddedOnBodyEvent((woundId, wound), parentWoundable, woundableRoot);
                RaiseLocalEvent(bodyUid, ref bodyEv);
            }
        }

        Dirty(childEntity, childWoundable);
    }

    private void InternalRemoveWoundableFromParent(
        EntityUid parentEntity,
        EntityUid childEntity,
        WoundableComponent parentWoundable,
        WoundableComponent childWoundable)
    {
        if (TerminatingOrDeleted(childEntity)
            || TerminatingOrDeleted(parentEntity))
            return;

        parentWoundable.ChildWoundables.Remove(childEntity);
        childWoundable.ParentWoundable = null;
        childWoundable.RootWoundable = childEntity;

        FixWoundableRoots(childEntity, childWoundable);

        if (!TryComp<WoundableComponent>(parentWoundable.RootWoundable, out var oldWoundableRoot))
            return;

        var woundableDetached = new WoundableDetachedEvent(parentEntity, parentWoundable);

        RaiseLocalEvent(childEntity, ref woundableDetached);

        foreach (var (woundId, wound) in GetAllWounds(childEntity, childWoundable))
        {
            var ev = new WoundRemovedEvent(wound, childWoundable, oldWoundableRoot);
            RaiseLocalEvent(woundId, ref ev);

            var ev2 = new WoundRemovedEvent(wound, childWoundable, oldWoundableRoot);
            RaiseLocalEvent(childWoundable.RootWoundable, ref ev2);
        }

        Dirty(childEntity, childWoundable);
    }

    private void FixWoundableRoots(EntityUid targetEntity, WoundableComponent targetWoundable)
    {
        if (targetWoundable.ChildWoundables.Count == 0)
            return;

        foreach (var (childEntity, childWoundable) in GetAllWoundableChildren(targetEntity, targetWoundable))
        {
            childWoundable.RootWoundable = targetWoundable.RootWoundable;
            Dirty(childEntity, childWoundable);
        }

        Dirty(targetEntity, targetWoundable);
    }

    private void CheckSeverityThresholds(EntityUid wound,
        EntityUid woundable,
        WoundComponent? component = null,
        WoundableComponent? woundableComp = null)
    {
        if (!Resolve(wound, ref component, false)
            || !Resolve(woundable, ref woundableComp)
            || !_net.IsServer)
            return;

        var nearestSeverity = component.WoundSeverity;
        foreach (var (severity, value) in _woundThresholds.OrderByDescending(kv => kv.Value))
        {
            var scaledThreshold = value * (woundableComp.IntegrityCap / 100);
            if (component.WoundSeverityPoint < scaledThreshold)
                continue;

            if (severity == WoundSeverity.Healed && component.WoundSeverityPoint > 0)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity != component.WoundSeverity)
        {
            var ev = new WoundSeverityChangedEvent(component.WoundSeverity, nearestSeverity);
            RaiseLocalEvent(wound, ref ev);
        }
        component.WoundSeverity = nearestSeverity;

        if (!TerminatingOrDeleted(component.HoldingWoundable))
        {
            _appearance.SetData(component.HoldingWoundable,
                WoundableVisualizerKeys.Wounds,
                new WoundVisualizerGroupData(GetWoundableWounds(component.HoldingWoundable).Select(ent => GetNetEntity(ent)).ToList()));
        }
    }

    /// <summary>
    /// Checks if the current integrity crosses any severity thresholds and updates accordingly
    /// </summary>
    private void CheckWoundableSeverityThresholds(EntityUid woundable, WoundableComponent? component = null)
    {
        if (!Resolve(woundable, ref component, false))
            return;

        var nearestSeverity = component.WoundableSeverity;
        foreach (var (severity, value) in component.Thresholds.OrderByDescending(kv => kv.Value))
        {
            if (component.WoundableIntegrity >= component.IntegrityCap)
            {
                nearestSeverity = WoundableSeverity.Healthy;
                break;
            }

            if (component.WoundableIntegrity < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity != component.WoundableSeverity)
        {
            var ev = new WoundableSeverityChangedEvent(component.WoundableSeverity, nearestSeverity);
            RaiseLocalEvent(woundable, ref ev);
        }
        component.WoundableSeverity = nearestSeverity;

        Dirty(woundable, component);

        if (_body.GetBody(woundable) is not {} body)
            return;

        _bodyStatus.UpdateStatus(body);

        _appearance.SetData(woundable,
            WoundableVisualizerKeys.Wounds,
            new WoundVisualizerGroupData(GetWoundableWounds(woundable).Select(ent => GetNetEntity(ent)).ToList()));
    }

    private bool TryFumble(string message, SoundPathSpecifier sound, EntityUid body, float odds)
    {
        var rand = new System.Random((int) _timing.CurTick.Value);
        if (rand.NextFloat() < odds)
        {
            _popup.PopupClient(Loc.GetString(message), body, PopupType.Medium);
            var ev = new DropHandItemsEvent();
            RaiseLocalEvent(body, ref ev, false);
            _audio.PlayPredicted(sound, body, body);
            return true;
        }
        return false;
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
    private bool IsWoundPrototypeValid(string protoId)
    {
        // TODO SHITMED: HasComp<WoundComponent>(protoId)
        return _prototype.TryIndex<EntityPrototype>(protoId, out var woundPrototype)
               && woundPrototype.TryGetComponent<WoundComponent>(out _, Factory);
    }

    private void DestroyWoundableChildren(EntityUid woundableEntity,
        WoundableComponent? woundableComp = null,
        bool amputateChildrenSafely = false)
    {
        if (!Resolve(woundableEntity, ref woundableComp, false))
            return;

        foreach (var child in woundableComp.ChildWoundables)
        {
            var childWoundable = Comp<WoundableComponent>(child);
            if (childWoundable.WoundableSeverity is WoundableSeverity.Mangled)
            {
                DestroyWoundable(woundableEntity, child, childWoundable);
                continue;
            }

            if (amputateChildrenSafely)
                AmputateWoundableSafely(woundableEntity, child, childWoundable);
            else
                AmputateWoundable(woundableEntity, child, childWoundable);
        }
    }

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

            if (!TryComp<DamageableComponent>(part, out var damageable))
                continue;

            var nearestSeverity = WoundableSeverity.Severed;
            var damage = damageable.TotalDamage;

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

    public Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity> GetWoundableStatesOnBodyPainFeels(EntityUid body)
    {
        var result = SeveredStates();

        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (_body.GetCategory(part.Owner) is not {} category ||
                !TryComp<NerveComponent>(part, out var nerve))
                continue;

            var damageFeeling = part.Comp.WoundableIntegrity * nerve.PainFeels;

            var nearestSeverity = part.Comp.WoundableSeverity;
            foreach (var (severity, value) in part.Comp.Thresholds.OrderByDescending(kv => kv.Value))
            {
                if (damageFeeling <= 0)
                {
                    nearestSeverity = WoundableSeverity.Mangled;
                    break;
                }

                if (damageFeeling >= part.Comp.IntegrityCap)
                {
                    nearestSeverity = WoundableSeverity.Healthy;
                    break;
                }

                if (damageFeeling < value)
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
    /// Check if this woundable is root
    /// </summary>
    /// <param name="woundableEntity">Owner of the woundable</param>
    /// <param name="woundable">woundable component</param>
    /// <returns>true if the woundable is the root of the hierarchy</returns>
    public bool IsWoundableRoot(EntityUid woundableEntity, WoundableComponent? woundable = null)
    {
        return Resolve(woundableEntity, ref woundable, false)
            && woundable.RootWoundable == woundableEntity;
    }

    public FixedPoint2 GetTotalWoundSeverity(EntityUid body)
    {
        var total = FixedPoint2.Zero;
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            total += GetWoundableSeverityPoint(part, part.Comp);
        }

        return total;
    }

    /// <summary>
    /// Retrieves all wounds associated with a specified bodypart.
    /// </summary>
    /// <param name="targetEntity">The UID of the target entity.</param>
    /// <param name="targetWoundable">Optional: The WoundableComponent of the target entity.</param>
    /// <returns>An enumerable collection of tuples containing EntityUid and WoundComponent pairs.</returns>
    public IEnumerable<Entity<WoundComponent>> GetAllWounds(EntityUid targetEntity,
        WoundableComponent? targetWoundable = null)
    {
        if (!Resolve(targetEntity, ref targetWoundable, false))
            yield break;

        foreach (var (_, childWoundable) in GetAllWoundableChildren(targetEntity, targetWoundable))
        {
            if (childWoundable.Wounds == null)
                continue;

            foreach (var woundEntity in childWoundable.Wounds.ContainedEntities)
                yield return (woundEntity, _query.Comp(woundEntity));

        }
    }

    /// <summary>
    /// Gets all woundable children of a specified woundable
    /// </summary>
    /// <param name="targetEntity">Owner of the woundable</param>
    /// <param name="targetWoundable"></param>
    /// <returns>Enumerable to the found children</returns>
    public IEnumerable<Entity<WoundableComponent>> GetAllWoundableChildren(EntityUid targetEntity,
        WoundableComponent? targetWoundable = null)
    {
        if (!Resolve(targetEntity, ref targetWoundable, false))
            yield break;

        foreach (var childEntity in targetWoundable.ChildWoundables)
        {
            if (!TryComp(childEntity, out WoundableComponent? childWoundable))
                continue;
            foreach (var value in GetAllWoundableChildren(childEntity, childWoundable))
            {
                yield return value;
            }
        }

        yield return (targetEntity, targetWoundable);
    }

    /// <summary>
    /// Parents a woundable to another
    /// </summary>
    /// <param name="parentEntity">Owner of the new parent</param>
    /// <param name="childEntity">Owner of the woundable we want to attach</param>
    /// <param name="parentWoundable">The new parent woundable component</param>
    /// <param name="childWoundable">The woundable we are attaching</param>
    /// <returns>true if successful</returns>
    public bool AddWoundableToParent(
        EntityUid parentEntity,
        EntityUid childEntity,
        WoundableComponent? parentWoundable = null,
        WoundableComponent? childWoundable = null)
    {
        if (!Resolve(parentEntity, ref parentWoundable, false)
            || !Resolve(childEntity, ref childWoundable, false)
            || childWoundable.ParentWoundable == null)
            return false;

        InternalAddWoundableToParent(parentEntity, childEntity, parentWoundable, childWoundable);
        return true;
    }

    /// <summary>
    /// Removes a woundable from its parent (if present)
    /// </summary>
    /// <param name="parentEntity">Owner of the parent woundable</param>
    /// <param name="childEntity">Owner of the child woundable</param>
    /// <param name="parentWoundable"></param>
    /// <param name="childWoundable"></param>
    /// <returns>true if successful</returns>
    public bool RemoveWoundableFromParent(
        EntityUid parentEntity,
        EntityUid childEntity,
        WoundableComponent? parentWoundable = null,
        WoundableComponent? childWoundable = null)
    {
        if (!Resolve(parentEntity, ref parentWoundable, false)
            || !Resolve(childEntity, ref childWoundable, false)
            || childWoundable.ParentWoundable == null)
            return false;

        InternalRemoveWoundableFromParent(parentEntity, childEntity, parentWoundable, childWoundable);
        return true;
    }


    /// <summary>
    /// Finds all children of a specified woundable that have a specific component
    /// </summary>
    /// <param name="targetEntity"></param>
    /// <param name="targetWoundable"></param>
    /// <typeparam name="T">the type of the component we want to find</typeparam>
    /// <returns>Enumerable to the found children</returns>
    public IEnumerable<Entity<WoundableComponent, T>> GetAllWoundableChildrenWithComp<T>(EntityUid targetEntity,
        WoundableComponent? targetWoundable = null) where T: Component, new()
    {
        if (!Resolve(targetEntity, ref targetWoundable, false))
            yield break;

        foreach (var childEntity in targetWoundable.ChildWoundables)
        {
            if (!TryComp(childEntity, out WoundableComponent? childWoundable))
                continue;

            foreach (var value in GetAllWoundableChildrenWithComp<T>(childEntity, childWoundable))
            {
                yield return value;
            }
        }

        if (!TryComp(targetEntity, out T? foundComp))
            yield break;

        yield return (targetEntity, targetWoundable, foundComp);
    }

    /// <summary>
    /// Get the wounds present on a specific woundable
    /// </summary>
    /// <param name="targetEntity">Entity that owns the woundable</param>
    /// <param name="targetWoundable">Woundable component</param>
    /// <returns>An enumerable pointing to one of the found wounds</returns>
    public List<Entity<WoundComponent>> GetWoundableWounds(EntityUid targetEntity,
        WoundableComponent? targetWoundable = null)
    {
        if (!Resolve(targetEntity, ref targetWoundable, false)
            || targetWoundable.Wounds is not {} container
            || container.Count == 0)
            return [];

        var wounds = new List<Entity<WoundComponent>>();
        foreach (var wound in container.ContainedEntities)
        {
            wounds.Add((wound, _query.Comp(wound)));
        }
        return wounds;
    }

    /// <summary>
    /// Get the wounds present on a specific woundable, with a component you want
    /// </summary>
    /// <param name="targetEntity">Entity that owns the woundable</param>
    /// <param name="targetWoundable">Woundable component</param>
    /// <returns>An enumerable pointing to one of the found wounds, with the said component</returns>
    public List<Entity<WoundComponent, T>> GetWoundableWoundsWithComp<T>(
        EntityUid targetEntity,
        WoundableComponent? targetWoundable = null) where T : Component, new()
    {
        if (!Resolve(targetEntity, ref targetWoundable, false)
            || targetWoundable.Wounds is not {} container
            || container.Count == 0)
            return [];

        var query = GetEntityQuery<T>();
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in container.ContainedEntities)
        {
            if (!query.TryComp(wound, out var comp))
                continue;

            wounds.Add((wound, _query.Comp(wound), comp));
        }
        return wounds;
    }

    /// <summary>
    /// Checks for wounds on an entity that have exceeded their MangleSeverity threshold
    /// </summary>
    public bool HasWoundsExceedingMangleSeverity(EntityUid targetEntity, WoundableComponent? targetWoundable = null)
    {
        if (!Resolve(targetEntity, ref targetWoundable))
            return false;

        return GetWoundableWounds(targetEntity, targetWoundable)
            .Any(wound =>
                wound.Comp.MangleSeverity != null &&
                wound.Comp.WoundSeverity >= wound.Comp.MangleSeverity);
    }


    /// <summary>
    /// Returns you the sum of all wounds on this woundable
    /// </summary>
    /// <param name="targetEntity">The woundable uid</param>
    /// <param name="targetWoundable">The component</param>
    /// <param name="damageGroup">The damage group of said wounds</param>
    /// <param name="healable">Are the wounds supposed to be healable</param>
    /// <returns>The severity sum</returns>
    public FixedPoint2 GetWoundableSeverityPoint(
        EntityUid targetEntity,
        WoundableComponent? targetWoundable = null,
        string? damageGroup = null,
        bool healable = false,
        bool ignoreBlockers = false)
    {
        if (!Resolve(targetEntity, ref targetWoundable, false)
            || targetWoundable.Wounds == null
            || targetWoundable.Wounds.Count == 0)
            return FixedPoint2.Zero;

        if (healable)
        {
            return GetWoundableWounds(targetEntity, targetWoundable)
                .Where(wound => _prototype.Index(wound.Comp.DamageGroup)?.ID == damageGroup || damageGroup == null)
                .Where(wound => CanHealWound(wound, wound.Comp, ignoreBlockers))
                .Aggregate(FixedPoint2.Zero, (current, wound) => current + wound.Comp.WoundSeverityPoint);
        }

        return GetWoundableWounds(targetEntity, targetWoundable)
            .Where(wound => _prototype.Index(wound.Comp.DamageGroup)?.ID == damageGroup || damageGroup == null)
            .Aggregate(FixedPoint2.Zero, (current, wound) => current + wound.Comp.WoundSeverityPoint);
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
        EntityUid targetEntity,
        WoundableComponent? targetWoundable = null,
        string? damageGroup = null,
        bool healable = false,
        bool ignoreBlockers = false)
    {
        if (!Resolve(targetEntity, ref targetWoundable, false)
            || targetWoundable.Wounds == null
            || targetWoundable.Wounds.Count == 0)
            return FixedPoint2.Zero;

        var wounds = GetWoundableWounds(targetEntity, targetWoundable);
        if (damageGroup != null)
            wounds.RemoveAll(wound => _prototype.Index(wound.Comp.DamageGroup)?.ID != damageGroup);
        if (healable)
            wounds.RemoveAll(wound => !CanHealWound(wound, wound.Comp, ignoreBlockers));

        return wounds.Aggregate(FixedPoint2.Zero, (current, wound) => current + wound.Comp.WoundSeverityPoint);
    }

    #endregion
}
