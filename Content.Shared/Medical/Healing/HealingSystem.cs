// <Trauma>
using Content.Medical.Common.Body;
using Content.Medical.Common.Healing;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
// </Trauma>
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Medical.Healing;

public sealed class HealingSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly BodySystem _body = default!;
    // </Trauma>
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    // Goobstation start
    private ProtoId<OrganCategoryPrototype>[] _partHealingOrder =
    {
        "Head",
        "Torso",
        "ArmLeft",
        "HandLeft",
        "ArmRight",
        "HandRight",
        "LegLeft",
        "FootLeft",
        "LegRight",
        "FootRight"
    };
    // Goobstation end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<DamageableComponent, HealingDoAfterEvent>(OnDoAfter);
        // TODO SHITMED: bruh move this out of here
        SubscribeLocalEvent<BodyComponent, HealingDoAfterEvent>(OnBodyDoAfter); // Shitmed Change

    }

    private void OnDoAfter(Entity<DamageableComponent> target, ref HealingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp(args.Used, out HealingComponent? healing)
            || HasComp<BodyComponent>(target)) // Shitmed - let body handle it separately
            return;

        if (healing.DamageContainers is not null &&
            target.Comp.DamageContainerID is not null &&
            !healing.DamageContainers.Contains(target.Comp.DamageContainerID.Value))
        {
            return;
        }

        TryComp<BloodstreamComponent>(target, out var bloodstream);

        // Heal some bloodloss damage.
        if (healing.BloodlossModifier != 0 && bloodstream != null)
        {
            var isBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount((target.Owner, bloodstream), healing.BloodlossModifier);
            if (isBleeding != bloodstream.BleedAmount > 0)
            {
                var popup = (args.User == target.Owner)
                    ? Loc.GetString("medical-item-stop-bleeding-self")
                    : Loc.GetString("medical-item-stop-bleeding", ("target", Identity.Entity(target.Owner, EntityManager)));
                _popupSystem.PopupClient(popup, target, args.User);
            }
        }

        // Restores missing blood
        if (healing.ModifyBloodLevel != 0 && bloodstream != null)
            _bloodstreamSystem.TryModifyBloodLevel((target.Owner, bloodstream), -healing.ModifyBloodLevel); // Goobedit

        if (!_damageable.TryChangeDamage(target.Owner, healing.Damage * _damageable.UniversalTopicalsHealModifier, out var healed, true, origin: args.Args.User) && healing.BloodlossModifier != 0)
            return;

        var total = healed.GetTotal();

        // Re-verify that we can heal the damage.
        var dontRepeat = false;
        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.ReduceCount((args.Used.Value, stackComp), 1);

            if (_stacks.GetCount((args.Used.Value, stackComp)) <= 0)
                dontRepeat = true;
        }
        else
        {
            PredictedQueueDel(args.Used.Value);
        }

        if (target.Owner != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed {ToPrettyString(target.Owner):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        // Goobstation
        // Only play sound if this is not a body part (body parts are handled by OnBodyDoAfter)
        if (!HasComp<BodyComponent>(target.Owner))
            _audio.PlayPredicted(healing.HealingEndSound, target.Owner, args.User);

        // Logic to determine the whether or not to repeat the healing action
        args.Repeat = HasDamage((args.Used.Value, healing), target) && !dontRepeat;
        args.Handled = true;

        if (!args.Repeat)
        {
            _popupSystem.PopupClient(Loc.GetString("medical-item-finished-using", ("item", args.Used)), target.Owner, args.User);
            return;
        }

        // Update our self heal delay so it shortens as we heal more damage.
        if (args.User == target.Owner)
            args.Args.Delay = healing.Delay * GetScaledHealingPenalty(target.Owner, healing.SelfHealPenaltyMultiplier);
    }

    private bool HasDamage(Entity<HealingComponent> healing, Entity<DamageableComponent> target)
    {
        var damageableDict = target.Comp.Damage.DamageDict;
        var healingDict = healing.Comp.Damage.DamageDict;
        foreach (var type in healingDict)
        {
            if (damageableDict[type.Key].Value > 0)
            {
                return true;
            }
        }

        if (TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            // Is ent missing blood that we can restore?
            if (healing.Comp.ModifyBloodLevel > 0
                && _solutionContainerSystem.ResolveSolution(target.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
                && _bloodstreamSystem.GetBloodLevel((target, bloodstream)) < 1)
            {
                return true;
            }

            // Is ent bleeding and can we stop it?
            if (healing.Comp.BloodlossModifier < 0 && bloodstream.BleedAmount > 0)
            {
                return true;
            }
        }

        return false;
    }

    // TODO SHITMED: get rid of this from here
    /// <summary>
    /// Shitmed - Method <c>IsBodyDamaged</c> returns if a body part can be healed by the healing component. Returns false part is fully healed too.
    /// </summary>
    /// <param name="target">the target Entity</param>
    /// <param name="user">The person trying to heal. (optional)</param>
    /// <param name="healing">The healing component.</param>
    /// <param name="targetedPart">bypasses targeting system to specify a limb. Must be set if user is null. (optional)</param>
    /// <returns> Wether or not the targeted part can be healed. </returns>
    public bool IsBodyDamaged(Entity<BodyComponent> target, EntityUid? user, HealingComponent healing, EntityUid? targetedPart = null)
    {
        // try get targeted part from the user if not specified
        if (targetedPart == null && user != null)
        {
            var partEv = new GetTargetedPartEvent(target);
            RaiseLocalEvent(user.Value, ref partEv);
            targetedPart = partEv.Part;
        }

        // no limb can be targeted at all
        if (targetedPart is not {} part || !TryComp<DamageableComponent>(part, out var damageable))
        {
            _popupSystem.PopupClient(Loc.GetString("missing-body-part"), target, user, PopupType.MediumCaution);
            return false;
        }

        // see if there is any damage that can be healed
        if (healing.Damage.DamageDict.Keys
            .Any(damageKey => damageable.Damage.DamageDict[damageKey].Value > 0))
            return true;

        if (healing.BloodlossModifier == 0)
            return false;

        // see if there are any bleeding wounds to stop
        var ev = new CheckPartBleedingEvent();
        RaiseLocalEvent(part, ref ev);
        return ev.Bleeding;
    }

    /// <summary>
    ///     This function tries to return the first limb that has one of the damage type we are trying to heal
    ///     Returns true or false if next damaged part exists.
    /// </summary>
    public bool TryGetNextDamagedPart(EntityUid ent, HealingComponent healing, out EntityUid? part) // Goob edit: private => public, used in RepairableSystems.cs
    {
        part = null;
        if (!TryComp<BodyComponent>(ent, out var body))
            return false;

        foreach (var limb in _body.GetExternalOrgans(ent))
        {
            part = limb;
            if (IsBodyDamaged((ent, body), null, healing, limb))
                return true;
        }
        return false;
    }

    private void OnBodyDoAfter(EntityUid ent, BodyComponent comp, ref HealingDoAfterEvent args)
    {
        var dontRepeat = false;

        if (args.Handled || args.Cancelled ||
            args.Target is not {} target ||
            !TryComp(args.Used, out HealingComponent? healing))
            return;

        var partEv = new GetTargetedPartEvent(target);
        RaiseLocalEvent(args.User, ref partEv);
        if (partEv.Part is not {} targetedWoundable)
        {
            _popupSystem.PopupClient(
                Loc.GetString("medical-item-cant-use", ("item", args.Used)),
                ent,
                args.User,
                PopupType.MediumCaution);
            return;
        }

        if (!TryComp<DamageableComponent>(targetedWoundable, out var damageableComp))
            return;

        var healedBleed = false;
        //var canHeal = true; // Shitmed - not used
        var healedTotal = new DamageSpecifier(); // Goobstation
        FixedPoint2 modifiedBleedStopAbility = 0;
        // Heal some bleeds
        bool healedBleedLevel = false;
        if (healing.BloodlossModifier != 0)
        {
            // Goobstation start
            var bleedBefore = 0.0;
            if (TryComp<BloodstreamComponent>(ent, out var bloodstream))
                bleedBefore = bloodstream.BleedAmountFromWounds + bloodstream.BleedAmountNotFromWounds;
            healedBleed = bleedBefore > 0.0;
            var woundEv = new HealBleedingWoundsEvent(healing.BloodlossModifier, modifiedBleedStopAbility);
            RaiseLocalEvent(targetedWoundable, ref woundEv);
            modifiedBleedStopAbility = woundEv.BleedStopAbility;
            if (healing.BloodlossModifier + modifiedBleedStopAbility < 0.0)
                _bloodstreamSystem.TryModifyBleedAmount(ent, (healing.BloodlossModifier + modifiedBleedStopAbility).Float()); // Use the leftover bleed heal
            if (healedBleed)
                _popupSystem.PopupClient(bleedBefore + healing.BloodlossModifier <= 0.0
                        ? Loc.GetString("rebell-medical-item-stop-bleeding-fully")
                        : Loc.GetString("rebell-medical-item-stop-bleeding-partially"),
                    ent,
                    args.User);
            // Goobstation end
        }

        if (healing.ModifyBloodLevel != 0)
            healedBleedLevel = _bloodstreamSystem.TryModifyBloodLevel(ent, -healing.ModifyBloodLevel);

        //healedBleed = healedBleedWound || healedBleedLevel;

        // Goobstation start
        var leftoverHealAndTrauma = false;
        var leftoverHealAndBleed = false;
        var healingLeft = healing.Damage * _damageable.UniversalTopicalsHealModifier;
        if (TryComp<BodyComponent>(ent, out var bodyComp))
        {
            // Create parts to go over queue: targetted part -> head -> torso -> everything else
            // Iterate over the parts in the predefined order until we run out of parts or run out of healing
            var woundablesQueue = new Queue<EntityUid>();
            woundablesQueue.Enqueue(targetedWoundable);
            foreach (var category in _partHealingOrder)
            {
                if (_body.GetOrgan(ent, category) is {} organ)
                    woundablesQueue.Enqueue(organ);
            }
            while (woundablesQueue.Count > 0 && healingLeft.GetTotal() < 0.0)
            {
                targetedWoundable = woundablesQueue.Dequeue();
                var ev = new PartHealAttemptEvent();
                RaiseLocalEvent(targetedWoundable, ref ev);
                if (ev.Cancelled)
                {
                    // if it wasn't healed then a trauma blocked it? goida
                    leftoverHealAndTrauma |= !healedBleedLevel;
                    continue;
                }

                if (healing.BloodlossModifier == 0 && healing.ModifyBloodLevel >= 0 && ev.Bleeding)  // If the healing item has no bleeding heals, and its bleeding, we raise the alert. Goobstation edit
                {
                    leftoverHealAndBleed = true;
                    continue;
                }

                var damageChanged = _damageable.ChangeDamage(targetedWoundable, healingLeft, true, origin: args.User, ignoreBlockers: healedBleed || healing.BloodlossModifier == 0); // GOOBEDIT
                healedTotal -= damageChanged;
                healingLeft -= damageChanged;
            }
        }
        else
        {
            var healed = _damageable.ChangeDamage(ent, healing.Damage * _damageable.UniversalTopicalsHealModifier, true, origin: args.User);
            healingLeft -= healed;
        }

        var isAnyTypeFullyConsumed = healingLeft.DamageDict.Any(d => d.Value == 0);

        if (!healedBleed && !isAnyTypeFullyConsumed && (leftoverHealAndTrauma || leftoverHealAndBleed))
        {
            if (leftoverHealAndTrauma)
                _popupSystem.PopupClient(Loc.GetString("medical-item-requires-surgery-rebell", ("target", ent)), ent, args.User, PopupType.MediumCaution);
            else if (leftoverHealAndBleed) // the else is because would like to not pop both the popups at once, priority goes to the trauma popup
                _popupSystem.PopupClient(Loc.GetString("medical-item-cant-use-rebell", ("target", ent)), ent, args.User);
            return;
        }
        // Goobstation end

        // Re-verify that we can heal the damage.
        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.TryUse((args.Used.Value, stackComp), 1);

            if (_stacks.GetCount((args.Used.Value, stackComp)) <= 0)
                dontRepeat = true;
        }
        else
        {
            QueueDel(args.Used.Value);
        }

        if (ent != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed {EntityManager.ToPrettyString(ent):target} for {healedTotal.GetTotal():damage} damage"); // Goobstation
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed themselves for {healedTotal.GetTotal():damage} damage"); // Goobstation
        }
        _audio.PlayPredicted(healing.HealingEndSound, ent, ent, AudioParams.Default.WithVariation(0.125f).WithVolume(1f)); // Goob edit

        // Logic to determine whether or not to repeat the healing action
        args.Repeat = IsAnythingToHeal(args.User, ent, (args.Used.Value, healing)); // GOOBEDIT
        args.Handled = true;

        if (args.Repeat || dontRepeat)
            return;

        if (modifiedBleedStopAbility != -healing.BloodlossModifier)
            // Goobstation predicted --> client
            _popupSystem.PopupClient(Loc.GetString("medical-item-finished-using", ("item", args.Used)), ent, args.User, PopupType.Medium);
    }

    // Shitmed Change End
    private void OnHealingUse(Entity<HealingComponent> healing, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(healing, args.User, args.User))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(Entity<HealingComponent> healing, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(healing, args.Target.Value, args.User))
            args.Handled = true;
    }

    // Goobstation start
    private bool IsAnythingToHeal(EntityUid user, EntityUid target, Entity<HealingComponent> healing)
    {
        if (!TryComp<DamageableComponent>(target, out var targetDamage))
            return false;

        return HasDamage(healing, (target, targetDamage)) ||
            TryComp<BodyComponent>(target, out var bodyComp) && // I'm paranoid, sorry.
            IsBodyDamaged((target, bodyComp), user, healing.Comp) ||
            healing.Comp.ModifyBloodLevel > 0 // Special case if healing item can restore lost blood...
                && TryComp<BloodstreamComponent>(target, out var bloodstream)
                && _solutionContainerSystem.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
                && bloodSolution.Volume < bloodSolution.MaxVolume;
    }
    // Goobstation end

    private bool TryHeal(Entity<HealingComponent> healing, Entity<DamageableComponent?> target, EntityUid user)
    {
        if (!Resolve(target, ref target.Comp, false))
            return false;

        if (healing.Comp.DamageContainers is not null &&
            target.Comp.DamageContainerID is not null &&
            !healing.Comp.DamageContainers.Contains(target.Comp.DamageContainerID.Value))
        {
            return false;
        }

        if (user != target.Owner && !_interactionSystem.InRangeUnobstructed(user, target.Owner, popup: true))
            return false;

        if (TryComp<StackComponent>(healing, out var stack) && stack.Count < 1)
            return false;

        // Shitmed Change Start
        var anythingToDo =
            HasDamage(healing, (target.Owner, target.Comp)) ||
            TryComp<BodyComponent>(target, out var bodyComp) && // I'm paranoid, sorry.
            IsBodyDamaged((target, bodyComp), user, healing.Comp) ||
            healing.Comp.ModifyBloodLevel < 0 // Special case if healing item can restore lost blood... Goobstation edit
                && TryComp<BloodstreamComponent>(target, out var bloodstream)
                && _solutionContainerSystem.ResolveSolution(target.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
                && bloodSolution.Volume < bloodSolution.MaxVolume; // ...and there is lost blood to restore.

        if (!anythingToDo)
        {
            _popupSystem.PopupClient(Loc.GetString("medical-item-cant-use", ("item", healing.Owner)), healing, user);
            return false;
        }
        // Shitmed Change End
            // Goobstation Moved - to avoid audio spam
            //_audio.PlayPredicted(healing.Comp.HealingBeginSound, healing, user);

        var isNotSelf = user != target.Owner;

        if (isNotSelf)
        {
            // Show this to the target
            // Goobstation predicted --> client
            var msg = Loc.GetString("medical-item-popup-target", ("user", Identity.Entity(user, EntityManager)), ("item", healing.Owner));
            _popupSystem.PopupClient(msg, target, target, PopupType.Medium);
        }

        var delay = isNotSelf || healing.Comp.SelfHealPenaltyMultiplier == 0f // Trauma - fix healing toolbox taking 1000 years to use
            ? healing.Comp.Delay
            : healing.Comp.Delay * GetScaledHealingPenalty(target, healing.Comp.SelfHealPenaltyMultiplier);

        // Play sound when starting the healing action
        // Goobstation
        _audio.PlayPredicted(healing.Comp.HealingBeginSound, target, user);

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, delay, new HealingDoAfterEvent(), target, target: target, used: healing)
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    /// <summary>
    /// Scales the self-heal penalty based on the amount of damage taken
    /// </summary>
    /// <param name="ent">Entity we're healing</param>
    /// <param name="mod">Maximum modifier we can have.</param>
    /// <returns>Modifier we multiply our healing time by</returns>
    public float GetScaledHealingPenalty(Entity<DamageableComponent?, MobThresholdsComponent?> ent, float mod)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return mod;

        if (!_mobThresholdSystem.TryGetThresholdForState(ent, MobState.Critical, out var amount, ent.Comp2))
            return 1;

        var percentDamage = (float)(ent.Comp1.TotalDamage / amount);
        // <Trauma>
        var ev = new ModifySelfHealSpeedEvent();
        RaiseLocalEvent(ent, ref ev);
        percentDamage *= ev.Modifier;
        // </Trauma>

        //basically make it scale from 1 to the multiplier.
        var output = percentDamage * (mod - 1) + 1;
        return Math.Max(output, 1);
    }
}
