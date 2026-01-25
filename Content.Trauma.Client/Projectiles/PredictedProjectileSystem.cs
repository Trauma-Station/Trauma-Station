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
    private EntityQuery<HiddenProjectileComponent> _hiddenQuery;

    public override void Initialize()
    {
        base.Initialize();

        _hiddenQuery = GetEntityQuery<HiddenProjectileComponent>();

        SubscribeLocalEvent<ProjectileComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<HiddenProjectileComponent, AttemptPointLightToggleEvent>(OnAttemptLightToggle);
        SubscribeNetworkEvent<ShotPredictedProjectileEvent>(OnShotPredictedProjectile);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // incase the light gets added after it's shot??
        var query = EntityQueryEnumerator<HiddenProjectileComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemComp<PointLightComponent>(uid);
        }
    }

    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnAttemptLightToggle(Entity<HiddenProjectileComponent> ent, ref AttemptPointLightToggleEvent args)
    {
        // don't let it be turned on
        args.Cancelled |= args.Enabled;
    }

    private void OnShotPredictedProjectile(ShotPredictedProjectileEvent args)
    {
        var uid = GetEntity(args.Projectile);
        Log.Debug($"Got {ToPrettyString(uid)}");
        if (!uid.IsValid())
            return; // client may not have received the projectile state yet

        RemComp<SpriteComponent>(uid);
        EnsureComp<HiddenProjectileComponent>(uid);
        RemComp<PointLightComponent>(uid);
    }
}
