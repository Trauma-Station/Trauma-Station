using Content.Goobstation.Common.Projectiles;
using Content.Goobstation.Common.Weapons.Penetration;
using Content.Shared.Administration.Logs;
using Content.Shared.Destructible;
using Content.Shared.Effects;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Camera;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Projectiles;

/// <summary>
/// Handles predicting projectile hits.
/// This was previously only done serverside.
/// </summary>
public sealed class PredictedProjectileSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture || !args.OtherFixture.Hard
            || component.ProjectileSpent || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var target = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _projectile.SetShooter(uid, component, target);
            _gun.SetTarget(uid, null, out _); // Goobstation
            component.IgnoredEntities.Clear(); // Goobstation
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage * _damageable.UniversalProjectileDamageModifier, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructible.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }

        // <Goob>
        TargetBodyPart? targetPart = null;
        if (TryComp(uid, out ProjectileMissTargetPartChanceComponent? missComp) &&
            !missComp.PerfectHitEntities.Contains(target))
            targetPart = TargetBodyPart.Chest;
        // </Goob>
        var deleted = Deleted(target);

        if (_damageable.TryChangeDamage((target, damageableComponent), ev.Damage, out var damage, component.IgnoreResistances, origin: component.Shooter, targetPart: targetPart) && Exists(component.Shooter))
        {
            if (!deleted && _net.IsServer) // intentionally not predicting so you know if color flashes its 100% a hit
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {damage:damage} damage");

            // <Goob> - Splits penetration change if target have PenetratableComponent
            if (!TryComp<PenetratableComponent>(target, out var penetratable))
            {
                // If the object won't be destroyed, it "tanks" the penetration hit.
                if (damage.GetTotal() < damageRequired)
                {
                    component.ProjectileSpent = true;
                }

                if (!component.ProjectileSpent)
                {
                    component.PenetrationAmount += damageRequired;
                    // The projectile has dealt enough damage to be spent.
                    if (component.PenetrationAmount >= component.PenetrationThreshold)
                    {
                        component.ProjectileSpent = true;
                    }
                }
            }
            else
            {
                // Goobstation - Here penetration threshold count as "penetration health".
                // If it's lower than damage than penetation damage entity cause it deletes projectile
                if (component.PenetrationThreshold < penetratable.PenetrateDamage)
                {
                    component.ProjectileSpent = true;
                }

                component.PenetrationThreshold -= FixedPoint2.New(penetratable.PenetrateDamage);
                component.Damage *= (1 - penetratable.DamagePenaltyModifier);
            }
            // </Goob>

            // If penetration is to be considered, we need to do some checks to see if the projectile should stop.
            if (component.PenetrationThreshold != 0)
            {
                // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
                if (component.PenetrationDamageTypeRequirement != null)
                {
                    var stopPenetration = false;
                    foreach (var requiredDamageType in component.PenetrationDamageTypeRequirement)
                    {
                        if (!damage.DamageDict.Keys.Contains(requiredDamageType))
                        {
                            stopPenetration = true;
                            break;
                        }
                    }
                    if (stopPenetration)
                        component.ProjectileSpent = true;
                }

                // If the object won't be destroyed, it "tanks" the penetration hit.
                if (damage.GetTotal() < damageRequired)
                {
                    component.ProjectileSpent = true;
                }

                if (!component.ProjectileSpent)
                {
                    component.PenetrationAmount += damageRequired;
                    // The projectile has dealt enough damage to be spent.
                    if (component.PenetrationAmount >= component.PenetrationThreshold)
                    {
                        component.ProjectileSpent = true;
                    }
                }
            }
            else
            {
                component.ProjectileSpent = true;
            }
        }

        // <Goob>
        if (component.Penetrate)
        {
            component.IgnoredEntities.Add(target);
            component.ProjectileSpent = false; // Hardlight bow should be able to deal damage while piercing, no?
        }
        // </Goob>

        if (!deleted)
        {
            _gun.PlayImpactSound(target, damage, component.SoundHit, component.ForceSound);

            if (!args.OurBody.LinearVelocity.IsLengthZero() && _timing.IsFirstTimePredicted)
                _recoil.KickCamera(target, args.OurBody.LinearVelocity.Normalized());
        }

        if ((component.DeleteOnCollide && component.ProjectileSpent) || (component.NoPenetrateMask & args.OtherFixture.CollisionLayer) != 0) // Goobstation - Make x-ray arrows not penetrate blob
            PredictedQueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform) && _timing.IsFirstTimePredicted)
        {
            RaiseLocalEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)));
        }
    }
}
