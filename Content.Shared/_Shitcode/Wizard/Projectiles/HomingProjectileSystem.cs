// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Wizard.TimeStop;
using Content.Shared.Interaction;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Goobstation.Wizard.Projectiles;

public sealed class HomingProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<FrozenComponent> _frozenQuery;

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _frozenQuery = GetEntityQuery<FrozenComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query =
            EntityQueryEnumerator<HomingProjectileComponent, PhysicsComponent, TransformComponent, FixturesComponent>();
        while (query.MoveNext(out var uid, out var homing, out var physics, out var xform, out var fix))
        {
            if (homing.NextUpdate > curTime)
                continue;

            homing.NextUpdate = curTime + homing.HomingTime;

            if (_frozenQuery.HasComp(uid))
                continue;

            if (!_xformQuery.TryComp(homing.Target, out var targetXform))
                continue;

            var goalAngle = (_transform.GetMapCoordinates(targetXform).Position -
                             _transform.GetMapCoordinates(xform).Position).ToWorldAngle();

            var speed = float.MaxValue;
            if (homing.HomingSpeed != null)
                speed = MathHelper.DegreesToRadians(homing.HomingSpeed.Value);

            _rotate.TryRotateTo(uid, goalAngle, frameTime, homing.Tolerance, speed, xform);

            var projectileSpeed = physics.LinearVelocity.Length();
            var velocity = _transform.GetWorldRotation(xform).ToWorldVec() * projectileSpeed;
            _physics.SetLinearVelocity(uid, velocity, true, true, fix, physics);
        }
    }
}
