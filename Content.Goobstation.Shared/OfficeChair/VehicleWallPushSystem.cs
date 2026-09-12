// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Shared.Vehicles;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Goobstation.Shared.OfficeChair;

public sealed partial class VehicleWallPushSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private float _shoveRange;
    private float _shoveSpeed;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, GoobCVars.ShoveRange, x => _shoveRange = x, true);
        Subs.CVar(_cfg, GoobCVars.ShoveSpeed, x => _shoveSpeed = x, true);
    }

    [SubscribeLocalEvent]
    private void OnStrapped(EntityUid uid, VehicleWallPushComponent strap, ref StrappedEvent args)
    {
        if (!TryComp(uid, out VehicleWallPushComponent? comp))
            return;

        if (comp.KickAction == null)
            _actions.AddAction(args.Buckle.Owner, ref comp.KickAction, comp.ActionProto, uid);
    }

    [SubscribeLocalEvent]
    private void OnUnstrapped(EntityUid uid, VehicleWallPushComponent strap, ref UnstrappedEvent args)
    {
        if (!TryComp(uid, out VehicleWallPushComponent? comp))
            return;

        if (comp.KickAction != null)
            _actions.RemoveAction(args.Buckle.Owner, comp.KickAction);

        comp.KickAction = null;
    }

    [SubscribeLocalEvent]
    private void OnKick(EntityUid uid, VehicleWallPushComponent comp, ref VehicleWallPushActionEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp(uid, out VehicleComponent? vehicle) || vehicle.Driver != args.Performer)
            return;
        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        var from = _xform.GetMapCoordinates(uid);
        var to = _xform.ToMapCoordinates(args.Target);
        if (from.MapId != to.MapId)
            return;

        var aim = to.Position - from.Position;
        var aimLen = aim.Length();
        if (aimLen == 0)
            return;

        var dir = aim / aimLen;
        var ray = new CollisionRay(from.Position, dir, VehicleWallPushComponent.KickMask);

        if (_physics.IntersectRayWithPredicate(to.MapId, ray, comp.MaxDistance, x => x == vehicle.Driver || x == uid).FirstOrNull() is not { HitEntity: { } blocker })
            return;

        _audio.PlayPredicted(comp.RollSound, args.Performer, args.Performer);

        if (HasComp<PhysicsComponent>(blocker))
        {
            var userPos = from.Position;
            var targetPos = _xform.GetMapCoordinates(blocker).Position;
            var delta = targetPos - userPos;

            if (delta.LengthSquared() > 0f)
            {
                var pushVec = Vector2.Normalize(delta) * _shoveRange;
                _throwing.TryThrow(blocker, pushVec, _shoveRange * _shoveSpeed, args.Performer, animated: true, playSound: false);
            }
        }

        var addVel = -dir * comp.KickSpeed;
        _physics.SetLinearVelocity(uid, physics.LinearVelocity + addVel);
        args.Handled = true;
    }

}
