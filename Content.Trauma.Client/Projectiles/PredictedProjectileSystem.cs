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
    [Dependency] private readonly PointLightSystem _light = default!;

    private EntityQuery<PointLightComponent> _lightQuery;

    public override void Initialize()
    {
        base.Initialize();

        _lightQuery = GetEntityQuery<PointLightComponent>();

        SubscribeLocalEvent<ProjectileComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeNetworkEvent<ShotPredictedProjectileEvent>(OnShotPredictedProjectile);
    }

    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnShotPredictedProjectile(ShotPredictedProjectileEvent args)
    {
        var uid = GetEntity(args.Projectile);
        if (!uid.IsValid())
            return; // client may not have received the projectile state yet

        RemComp<SpriteComponent>(uid);
        // TODO: engine desync thing
        #if !DEBUG
        if (_lightQuery.TryComp(uid, out var light))
        {
            // TODO
            //EntityManager.SetComponentNetSync(uid, light, false);
            light.NetSyncEnabled = false; // don't let server show it again
            _light.SetEnabled(uid, false, light);
        }
        #endif
    }
}
