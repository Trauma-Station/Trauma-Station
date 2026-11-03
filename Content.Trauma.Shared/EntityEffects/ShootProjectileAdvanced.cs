using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityEffects;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that shoots advanced projectile patterns in random directions, from the user's location
/// </summary>
public sealed partial class ShootProjectileAdvanced : EntityEffectBase<ShootProjectileAdvanced>
{
    /// <summary>
    /// The projectile prototype to shoot
    /// </summary>
    [DataField]
    public EntProtoId<ProjectileComponent> Projectile;

    /// <summary>
    /// How many projectiles to shoot
    /// </summary>
    [DataField]
    public int Amount = 1;

    /// <summary>
    /// Works with <see cref="Spread"/>
    /// </summary>
    [DataField]
    public int FullAngle = 360;

    /// <summary>
    /// This will calculate the direction based on <see cref="FullAngle"/>
    /// </summary>
    [DataField]
    public bool Spread;

    /// <summary>
    /// When <see cref="Spread"/> is false, extra angles for spread
    /// </summary>
    [DataField]
    public MinMax SpreadAngles;

    /// <summary>
    /// The speed of the projectiles
    /// </summary>
    [DataField]
    public float Speed = 40;
}

public sealed partial class ShootProjectileAdvancedEffectSystem : EntityEffectSystem<TransformComponent, ShootProjectileAdvanced>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ShootProjectileAdvanced> args)
    {
        var effect = args.Effect;
        var amount = effect.Amount;
        var speed = effect.Speed;
        var fullAngle = effect.FullAngle;
        var spread = effect.Spread;

        if (amount == 0)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));

        // Spread unique behavior
        if (spread)
        {
            var segmentAngle =  fullAngle/ amount;
            var shootCount = 0;
            for (var i = 0; i < amount; i++)
            {
                var angleMin = segmentAngle * shootCount;
                var angleMax = segmentAngle * (shootCount + 1);
                var angle = Angle.FromDegrees(rand.Next(angleMin, angleMax));
                shootCount++;

                var direction = angle.ToVec().Normalized();
                var proj = PredictedSpawnAtPosition(effect.Projectile, entity.Comp.Coordinates);
                _gun.ShootProjectile(
                    proj,
                    direction,
                    Vector2.Zero,
                    entity.Owner,
                    args.User,
                    speed);
            }

            return;
        }

        var spreadAngles = effect.SpreadAngles;
        var randVec = rand.NextAngle().ToVec();
        for (var i = 0; i < amount; i++)
        {
            var proj = PredictedSpawnAtPosition(effect.Projectile, entity.Comp.Coordinates);
            var randVecSpread = rand.NextAngle(spreadAngles.Min, spreadAngles.Max).ToVec();

            _gun.ShootProjectile(proj, randVec + randVecSpread, Vector2.Zero, entity.Owner, args.User, speed);
        }
    }
}
