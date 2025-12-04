// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Interaction;

public sealed class TelekinesisSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTetherGunSystem _tether = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AdminFrozenComponent> _frozenQuery;
    private EntityQuery<TelekineticInteractableComponent> _targetQuery;
    private EntityQuery<TetherGunComponent> _tetherGunQuery;

    public override void Initialize()
    {
        base.Initialize();

        _frozenQuery = GetEntityQuery<AdminFrozenComponent>();
        _targetQuery = GetEntityQuery<TelekineticInteractableComponent>();
        _tetherGunQuery = GetEntityQuery<TetherGunComponent>();

        // this is evil but preferable to making a new event to uncancel interaction attempts.
        // anything important that might accidentally get overriden (admin freeze) is already checked in CanUseTelekinesis
        SubscribeLocalEvent<TelekinesisComponent, InteractionAttemptEvent>(OnInteractionAttempt,
            after: new[] { typeof(SharedStunSystem), typeof(SharedCuffableSystem) });
        SubscribeLocalEvent<TelekinesisComponent, InRangeOverrideEvent>(OnRangeOverride);
        SubscribeLocalEvent<TelekinesisComponent, TelekinesisActionEvent>(OnAction);
        SubscribeLocalEvent<TelekinesisComponent, SleepStateChangedEvent>(OnSleepStateChanged);
        SubscribeLocalEvent<TelekinesisComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInteractionAttempt(Entity<TelekinesisComponent> ent, ref InteractionAttemptEvent args)
    {
        // overwrite previous cancel from stunned, cuffed etc
        args.Cancelled = !CanUseTelekinesis(ent);
    }

    private void OnRangeOverride(Entity<TelekinesisComponent> ent, ref InRangeOverrideEvent args)
    {
        args.Handled = true;
        // allow interacting from any range if it has TelekineticInteractable
        args.InRange = _targetQuery.HasComp(args.Target) ||
            IsInRange(args.User, args.Target, args.Range);
    }

    private void OnAction(Entity<TelekinesisComponent> ent, ref TelekinesisActionEvent args)
    {
        if (!_tetherGunQuery.TryComp(ent, out var gun))
            return;

        args.Handled = true;
        var original = gun.Tethered;
        _tether.StopTether(ent, gun);

        // chud shit doesnt predict anything :(
        if (_net.IsClient) return;

        // don't tether if you use action on the same item twice, or if you use it on yourself (easy cancel)
        if (args.Target != original && args.Target != ent.Owner)
            _tether.TryTether(ent, args.Target, args.Performer, gun);
    }

    // can't use your mind powers if you go eepy
    private void OnSleepStateChanged(Entity<TelekinesisComponent> ent, ref SleepStateChangedEvent args)
    {
        if (!args.FellAsleep)
            return;

        if (_tetherGunQuery.TryComp(ent, out var gun))
            _tether.StopTether(ent, gun);
    }

    // can't use your mind powers if you fucking die
    private void OnMobStateChanged(Entity<TelekinesisComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            return;

        if (_tetherGunQuery.TryComp(ent, out var gun))
            _tether.StopTether(ent, gun);
    }

    public bool CanUseTelekinesis(EntityUid uid)
    {
        // never let players bypass admin freeze
        if (_frozenQuery.HasComp(uid))
            return false;

        // can't use telekinesis if you are eepy
        return _blocker.CanConsciouslyPerformAction(uid);
    }

    public bool IsInRange(EntityUid user, EntityUid target, float range)
    {
        var xform = Transform(user);
        var targetXform = Transform(target);
        if (xform.MapUid != targetXform.MapUid)
            return false; // telekinetic not fucking god

        var pos = _transform.GetMapCoordinates(user, xform).Position;
        var targetPos = _transform.GetMapCoordinates(target, targetXform).Position;
        var dist2 = (pos - targetPos).LengthSquared();
        var r2 = range * range;
        return dist2 <= r2;
    }
}
