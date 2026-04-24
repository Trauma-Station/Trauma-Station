// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Vampires;

public sealed class VampireBloodsuckingSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly VampireSystem _vampire = default!;
    [Dependency] private readonly EntityQuery<TargetingComponent> _targetingQuery = default!;
    [Dependency] private readonly EntityQuery<VampireDrainableComponent> _drainableQuery = default!;
    [Dependency] private readonly EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    private static TimeSpan _bloodsuckingDelay = TimeSpan.FromSeconds(5); // TODO: Should be a cvar
    // TODO: Implement a delay before attempts so it doesn't get spammed

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

        // Target must be alive and be drainable, plus we must meet the requirements
        if (!_mobState.IsAlive(target) || !_drainableQuery.HasComp(target) || !CanBloodSuck(ent.Owner))
            return;

        BloodSuck(ent, target);
    }

    private void OnBloodSuckDoAfter(Entity<VampireBloodsuckingComponent> ent, ref BloodSuckDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Target is not { } target
            || !_drainableQuery.TryComp(target, out var drainable)
            || !_bloodstreamQuery.TryComp(target, out var bloodstream))
        {
            return;
        }

        var user = ent.Owner;
        var blood = (target, bloodstream);

        // If we have already reached our limit on this target,
        // then just satiate our hunger and stop
        if (drainable.BloodGathered >= drainable.MaxBlood)
        {
            _hunger.ModifyHunger(user, 100f); // TODO: Store in comp
            _popup.PopupClient("You have drained most of their life force, you will get no more usable blood from them", user, user, PopupType.MediumCaution);
            return;
        }


        // <Todo> Something is bugged with this
        if (!_solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var sol)
            || sol.AvailableVolume <= 0)
            return;
        Log.Debug($"Blood Volume: {sol.AvailableVolume}");

        var bloodToRemove = (int) FixedPoint2.Min(25f, sol.Volume);
        _bloodstream.TryBleedOut(blood, bloodToRemove);
        // </Todo>

        drainable.BloodGathered += bloodToRemove;
        Dirty(target, drainable);

        // Transfer the blood to the vampire's usable blood and total blood.
        _vampire.AdjustBlood(user, bloodToRemove); // TODO: Event here not method

        Log.Debug($"Vampire has received: {bloodToRemove}");
    }

    #region  Helper
    /// <summary>
    /// Starts the blood sucking process via DoAfter.
    /// </summary>
    private void BloodSuck(Entity<VampireBloodsuckingComponent> ent, EntityUid target)
    {
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
        if (!_targetingQuery.TryComp(user, out var targeting) || targeting.Target != TargetBodyPart.Head)
            return false;

        return true;
    }
    #endregion
}
