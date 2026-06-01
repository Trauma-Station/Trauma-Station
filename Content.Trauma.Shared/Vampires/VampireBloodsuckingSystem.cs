// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Vampires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Vampires;

public sealed partial class VampireBloodsuckingSystem : EntitySystem
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private EntityQuery<MindContainerComponent> _mindQuery = default!;
    [Dependency] private EntityQuery<TargetingComponent> _targetingQuery = default!;
    [Dependency] private EntityQuery<VampireDrainableComponent> _drainableQuery = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    private static readonly EntProtoId BiteEffect = "WeaponArcBite";
    private static readonly SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireBloodsuckingComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<VampireBloodsuckingComponent, BloodSuckDoAfterEvent>(OnBloodSuckDoAfter);
    }

    private void OnMeleeHit(Entity<VampireBloodsuckingComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var target = args.HitEntities.First();

        // Target must be alive and be drainable,
        // plus we must meet the requirements
        if (!_mobState.IsAlive(target) || !_drainableQuery.HasComp(target) || !CanBloodSuck(ent.Owner))
            return;

        var attemptEv = new BloodsuckingAttemptEvent();
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _popup.PopupClient("This target cannot be drained!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        if (!_ingestion.HasMouthAvailable(target, ent.Owner))
            return;

        BloodSuck(ent, target);

        // Cancel the normal hit interaction,
        // we don't want to continue the behavior.
        args.Handled = true;
    }

    private void OnBloodSuckDoAfter(Entity<VampireBloodsuckingComponent> ent, ref BloodSuckDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target || !_drainableQuery.TryComp(target, out var drainable))
            return;

        if (!_bloodstreamQuery.TryComp(target, out var bloodstream))
            return;

        var bloodEnt = (target, bloodstream);
        if (!_solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var sol) || sol.Volume <= 0)
            return;

        var bloodToRemove = FixedPoint2.Min(ent.Comp.BloodToRemove, sol.Volume);
        var bloodInt = (int) bloodToRemove;

        _bloodstream.TryModifyBloodLevel(bloodEnt, bloodToRemove);
        _bloodstream.TryModifyBleedAmount(bloodEnt, bloodEnt.bloodstream.MaxBleedAmount * 0.6f);

        var user = ent.Owner;
        _hunger.ModifyHunger(user, ent.Comp.HungerRestoration);

        // animals and no mind can't give you total/usable blood
        // testing against the mindcontainer component directly fixes mispredicts of _mind.TryGetMind
        if (!_mindQuery.TryComp(target, out var mindContainer) || !mindContainer.HasMind)
        {
            _popup.PopupClient("Their blood is pale...", user, user, PopupType.MediumCaution);
            return;
        }

        // If we have already reached our limit on this target,
        // then don't go further.
        if (drainable.BloodGathered >= drainable.MaxBlood)
        {
            _popup.PopupClient("You have drained most of their life force, you will get no more usable blood from them", user, user, PopupType.MediumCaution);
            return;
        }

        drainable.BloodGathered += bloodInt;
        Dirty(target, drainable);

        // Notify anyone, for example Vampires to update their blood pools
        var ev = new BloodsuckingSuccessEvent(bloodInt, target);
        RaiseLocalEvent(user, ref ev);

        _popup.PopupClient("You drain the life force out of them...", user, user, PopupType.MediumCaution);
        _popup.PopupEntity("You feel like your life force has been drained...", user, target, PopupType.MediumCaution);

        ent.Comp.ConsumedVictims.Add(target);
        Dirty(ent);
    }

    #region  Helper
    /// <summary>
    /// Starts the blood sucking process via DoAfter.
    /// </summary>
    private void BloodSuck(Entity<VampireBloodsuckingComponent> ent, EntityUid target)
    {
        PredictedSpawnAtPosition(BiteEffect, Transform(target).Coordinates);
        _audio.PlayPredicted(BiteSound, target, ent.Owner);

        _popup.PopupClient("You start draining them...", ent.Owner, ent.Owner, PopupType.Medium);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user: ent.Owner,
            delay: ent.Comp.BloodsuckingDelay,
            @event: new BloodSuckDoAfterEvent(),
            eventTarget: ent.Owner,
            target: target
        )
        {
            BlockDuplicate = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupClient("The blood sucking process has failed!", ent.Owner, ent.Owner, PopupType.SmallCaution);
            Dirty(ent);
        }
    }
    #endregion

    /// <summary>
    /// Checks whether an entity can do a blood sucking sequence.
    /// </summary>
    /// <returns></returns>
    public bool CanBloodSuck(EntityUid user)
    {
        // Our current selected hand must be empty for this to work.
        if (!_hands.ActiveHandIsEmpty(user) || _mobState.IsCritical(user))
            return false;

        // We must be targeting our target's head first.
        // Note: This will disallow a normal head targeting interaction, but it's fine if your active hand is not empty.
        if (!_targetingQuery.TryComp(user, out var targeting) || targeting.Target != TargetBodyPart.Head)
            return false;

        return true;
    }
}

/// <summary>
/// Raised on the <see cref="VampireBloodsuckingComponent"/> entity, after the bloodsucking process starts.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BloodSuckDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on the entity that does the bloodsucking sequence, and it passes.
/// </summary>
/// <param name="BloodRemoved"></param>The blood that was removed from the target during the bloodsucking sequence.
[ByRefEvent]
public record struct BloodsuckingSuccessEvent(int BloodRemoved, EntityUid TargetSucked);

/// <summary>
/// Raised on the target to validate whether they can be drained of their blood.
/// </summary>
[ByRefEvent]
public record struct BloodsuckingAttemptEvent(bool Cancelled = false);
