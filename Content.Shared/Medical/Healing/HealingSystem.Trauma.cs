using Content.Medical.Common.Body;
using Content.Medical.Common.Healing;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Medical.Healing;

public sealed partial class HealingSystem
{
    [Dependency] private readonly BodySystem _body = default!;

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
        "FootRight",
        "Tail",
        "Wings"
    };

    private bool AnyHealable(Entity<DamageableComponent?> part, DamageSpecifier healing)
    {
        var damage = _damageable.GetAllDamage(part);
        foreach (var type in healing.DamageDict.Keys)
        {
            if (damage.DamageDict.TryGetValue(type, out var value) && value > 0)
                return true;
        }

        return false;
    }

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

    /// <summary>
    /// Returns if a body part can be healed by the healing component. Returns false part is fully healed too.
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
        if (AnyHealable((part, damageable), healing.Damage))
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
                $"{ToPrettyString(args.User):user} healed {ToPrettyString(ent):target} for {healedTotal.GetTotal():damage} damage"); // Goobstation
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed themselves for {healedTotal.GetTotal():damage} damage"); // Goobstation
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
}
