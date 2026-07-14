// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map;
using Content.Trauma.Common.Throwing;
using Content.Shared.Damage.Systems;
using Content.Shared.Item;

namespace Content.Goobstation.Shared.Boomerang;

public sealed partial class BoomerangSystem : EntitySystem
{
    [Dependency] private ThrowingSystem _throwingSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;

    private List<(EntityUid, EntityCoordinates, float, EntityUid?)> _toThrow = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BoomerangComponent, ThrowDoHitEvent>(OnHit,
            after: new[] { typeof(SharedDamageOtherOnHitSystem) });
    }

    [SubscribeLocalEvent]
    private void OnPickUpAttempt(Entity<BoomerangComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.Thrower is { } thrower && args.User != thrower)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamageOnHit(Entity<BoomerangComponent> ent, ref BeforeDamageOtherOnHitEvent args)
    {
        if (ent.Comp.IsReturning)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<BoomerangComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out ThrownItemComponent? thrown) || thrown.Thrower is not { } user)
            return;

        SetThrower(ent, user);
    }

    private void OnHit(Entity<BoomerangComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp(args.Thrown, out PhysicsComponent? body) || args.Component.Thrower is not { } thrower)
            return;

        var ourCoords = _transform.GetMapCoordinates(args.Thrown);
        var throwerCoords = _transform.GetMapCoordinates(thrower);

        if (ourCoords.MapId != throwerCoords.MapId)
            return;

        var vec = (throwerCoords.Position - ourCoords.Position).Normalized() * body.LinearVelocity.Length();

        _physics.SetLinearVelocity(args.Thrown, vec, body: body);

        ent.Comp.IsReturning = true;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (uid, coords, speed, thrower) in _toThrow)
        {
            if (TerminatingOrDeleted(uid) || thrower != null && TerminatingOrDeleted(thrower))
                continue;

            _physics.SetLinearVelocity(uid, Vector2.Zero);
            _throwingSystem.TryThrow(uid, coords, speed, user: thrower, recoil: false, playSound: false);
        }

        _toThrow.Clear();
    }

    [SubscribeLocalEvent]
    private void OnThrown(Entity<BoomerangComponent> ent, ref ThrownEvent args)
    {
        if (ent.Comp.Thrower == null)
            SetThrower(ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnLanded(Entity<BoomerangComponent> ent, ref LandEvent args)
    {
        if (ent.Comp.Thrower == null)
            return;

        var thrower = ent.Comp.Thrower.Value;

        if (TerminatingOrDeleted(thrower) || ent.Comp.CurrentHops >= ent.Comp.MaxHops)
        {
            SetThrower(ent, null);
            return;
        }

        var xform = Transform(ent);
        var throwerXform = Transform(thrower);

        if (!xform.Coordinates.TryDistance(EntityManager, throwerXform.Coordinates, out var distance))
        {
            SetThrower(ent, null);
            return;
        }

        if (distance < ent.Comp.PickupDistance)
        {
            SetThrower(ent, null); // don't throw it anymore
            _handsSystem.TryPickup(thrower, ent);
            return;
        }

        // everything is fine and it's out-of-range, re-throw to thrower on next frame (or it breaks)
        _toThrow.Add((ent, throwerXform.Coordinates, ent.Comp.ReturnSpeed, thrower));
        ent.Comp.CurrentHops++;
    }

    /// <summary>
    /// Sets the entity a boomerang should return to and resets the hops counter
    /// </summary>
    public void SetThrower(Entity<BoomerangComponent> ent, EntityUid? newThrower)
    {
        ent.Comp.Thrower = newThrower;
        ent.Comp.CurrentHops = 0;
        ent.Comp.IsReturning = false;
        Dirty(ent);
    }
}
