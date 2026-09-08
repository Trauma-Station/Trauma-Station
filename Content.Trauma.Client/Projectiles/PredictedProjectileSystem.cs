// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Content.Trauma.Shared.Projectiles;
using Robust.Client.Physics;

namespace Content.Trauma.Client.Projectiles;

/// <summary>
/// Hides the server-spawned projectile when firing a predicted gun.
/// </summary>
public sealed partial class PredictedProjectileSystem : EntitySystem
{
    [Dependency] private PhysicsSystem _physics = default!;

    [SubscribeLocalEvent]
    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    [SubscribeNetworkEvent]
    private void OnShotPredictedProjectile(ShotPredictedProjectileEvent args)
    {
        var uid = GetEntity(args.Projectile);
        if (!uid.IsValid())
            return;

        _physics.UpdateIsPredicted(uid);
        // TODO: come up with solution to fix the jitter when clientside entity is deleted and serverside one is spawned back at the shooter
        // clientside components have no way to persist so this may need engine work
    }
}
