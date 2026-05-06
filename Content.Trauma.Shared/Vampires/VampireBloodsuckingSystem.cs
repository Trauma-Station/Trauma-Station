// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Vampires;

public sealed class VampireBloodsuckingSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly EntityQuery<TargetingComponent> _targetingQuery = default!;
    [Dependency] private readonly EntityQuery<VampireDrainableComponent> _drainableQuery = default!;
    [Dependency] private readonly EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    private static readonly EntProtoId BiteEffect = "WeaponArcBite";                                   // TODO: This sprite is ass, change it to custom one
    private static readonly SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg"); // TODO: This sound is ass, change it to custom one

    private static TimeSpan _bloodsuckingDelay = TimeSpan.FromSeconds(5); // TODO: Should be a cvar

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

        var attemptEv = new BloodsuckingAttemptEvent();
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _popup.PopupClient("This target cannot be drained!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        // Target must be alive and be drainable,
        // plus we must meet the requirements
        if (!_mobState.IsAlive(target) || !_drainableQuery.HasComp(target) || !CanBloodSuck(ent.Owner))
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

        var user = ent.Owner;
        _hunger.ModifyHunger(user, ent.Comp.HungerRestoration);

        // If we have already reached our limit on this target,
        // then just satiate our hunger and stop
        if (drainable.BloodGathered >= drainable.MaxBlood)
        {
            _popup.PopupClient("You have drained most of their life force, you will get no more usable blood from them", user, user, PopupType.MediumCaution);
            return;
        }

        if (!_bloodstreamQuery.TryComp(target, out var bloodstream))
            return;

        var bloodEnt = (target, bloodstream);
        if (!_solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var sol) || sol.Volume <= 0)
            return;

        var bloodToRemove = FixedPoint2.Min(ent.Comp.BloodToRemove, sol.Volume);
        var bloodInt = (int) bloodToRemove;

        _bloodstream.TryModifyBloodLevel(bloodEnt, bloodToRemove);
        _bloodstream.TryModifyBleedAmount(bloodEnt, bloodEnt.bloodstream.MaxBleedAmount * 0.6f); // TODO: Adjust this somewhere where it feels good

        drainable.BloodGathered += bloodInt;
        Dirty(target, drainable);

        // Notify anyone, for example Vampires to update their blood pools
        var ev = new BloodsuckingSuccessEvent(bloodInt);
        RaiseLocalEvent(user, ref ev);

        _popup.PopupClient("You drain the life force out of them...", user, user, PopupType.MediumCaution);
        _popup.PopupEntity("You feel like your life force has been drained...", target, target, PopupType.MediumCaution);
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
            delay: _bloodsuckingDelay,
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

    /// <summary>
    /// Checks whether an entity can do a blood sucking sequence.
    /// </summary>
    /// <returns></returns>
    private bool CanBloodSuck(EntityUid user)
    {
        // Our current selected hand must be empty for this to work.
        if (!_hands.ActiveHandIsEmpty(user))
            return false;

        // We must be targeting our target's head first.
        // Note: This will disallow a normal head targeting interaction, but it's fine if your active hand is not empty.
        if (!_targetingQuery.TryComp(user, out var targeting) || targeting.Target != TargetBodyPart.Head)
            return false;

        return true;
    }
    #endregion
}
