// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Projectiles;
using Content.Trauma.Shared.Projectiles;
using Robust.Client.GameObjects;
using Robust.Client.Physics;

namespace Content.Trauma.Client.Projectiles;

/// <summary>
/// Hides the server-spawned projectile when firing a predicted gun.
/// </summary>
public sealed class PredictedProjectileSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<DeletingProjectileEvent>(OnDeletingProjectile);
        SubscribeNetworkEvent<ShotPredictedProjectileEvent>(OnShotPredictedProjectile);
    }

    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnDeletingProjectile(ref DeletingProjectileEvent args)
    {
        RemComp<SpriteComponent>(args.Entity);
        RemComp<PointLightComponent>(args.Entity);
    }

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
