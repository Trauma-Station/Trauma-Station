// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Common.Projectiles;
using Content.Goobstation.Common.Weapons.Penetration;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Administration.Logs;
using Content.Shared.Destructible;
using Content.Shared.Effects;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
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

    private EntityQuery<ProjectileComponent> _query;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<ProjectileComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture || !args.OtherFixture.Hard)
            return;

        DoHit((uid, component, args.OurBody), args.OtherEntity, args.OtherFixture);
    }

    /// <summary>
    /// Process a hit for a projectile and a target entity.
    /// This overload uses the first hard fixture on the target,
    /// there should only be 1 hard fixture on a given entity.
    /// Checking multiple hard fixtures would need a collision layer to check against, CBF.
    /// </summary>
    public void DoHit(EntityUid uid, EntityUid target)
    {
        if (!_query.TryComp(uid, out var comp) ||
            !_physicsQuery.TryComp(uid, out var physics) ||
            FindHardFixture(target) is not {} otherFixture)
            return;

        DoHit((uid, comp, physics), target, otherFixture);
    }

    private Fixture? FindHardFixture(EntityUid uid)
    {
        if (!_fixturesQuery.TryComp(uid, out var comp))
            return null;

        foreach (var fixture in comp.Fixtures.Values)
        {
            if (fixture.Hard)
                return fixture;
        }

        return null;
    }

    /// <summary>
    /// Process a hit for a projectile and a target entity.
    /// </summary>
    public void DoHit(Entity<ProjectileComponent, PhysicsComponent> ent, EntityUid target, Fixture otherFixture)
    {
        var (uid, comp, ourBody) = ent;
        if (comp.ProjectileSpent || comp is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, comp, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _projectile.SetShooter(uid, comp, target);
            _gun.SetTarget(uid, null, out _); // Goobstation
            comp.IgnoredEntities.Clear(); // Goobstation
            return;
        }

        var shooter = comp.Shooter;
        var ev = new ProjectileHitEvent(comp.Damage * _damageable.UniversalProjectileDamageModifier, target, shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructible.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageable))
        {
            damageRequired -= damageable.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }

        // <Goob>
        var targetPart = shooter is {} user
            ? _gun.GetTargetPart(user, target)
            : null;
        if (TryComp(uid, out ProjectileMissTargetPartChanceComponent? missComp) &&
            !missComp.PerfectHitEntities.Contains(target))
            targetPart = TargetBodyPart.Chest;
        // </Goob>
        var deleted = Deleted(target);

        if (_damageable.TryChangeDamage((target, damageable), ev.Damage, out var damage, comp.IgnoreResistances, origin: shooter, targetPart: targetPart) && Exists(shooter))
        {
            if (!deleted && _net.IsServer) // intentionally not predicting so you know if color flashes its 100% a hit
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(shooter):user} hit {otherName:target} and dealt {damage:damage} damage");

            comp.ProjectileSpent = !TryPenetrate((uid, comp), target, damage, damageRequired);
        }
        else
        {
            comp.ProjectileSpent = true;
        }

        // <Goob>
        if (comp.Penetrate)
        {
            comp.IgnoredEntities.Add(target);
            comp.ProjectileSpent = false; // Hardlight bow should be able to deal damage while piercing, no?
        }
        // </Goob>

        if (!deleted)
        {
            _gun.PlayImpactSound(target, damage, comp.SoundHit, comp.ForceSound);

            if (!ourBody.LinearVelocity.IsLengthZero() && _timing.IsFirstTimePredicted)
                _recoil.KickCamera(target, ourBody.LinearVelocity.Normalized());
        }

        if ((comp.DeleteOnCollide && comp.ProjectileSpent) || (comp.NoPenetrateMask & otherFixture.CollisionLayer) != 0) // Goobstation - Make x-ray arrows not penetrate blob
            PredictedQueueDel(uid);

        if (comp.ImpactEffect != null && TryComp(uid, out TransformComponent? xform) && _timing.IsFirstTimePredicted)
        {
            RaiseLocalEvent(new ImpactEffectEvent(comp.ImpactEffect, GetNetCoordinates(xform.Coordinates)));
        }
    }

    private bool TryPenetrate(Entity<ProjectileComponent> projectile, EntityUid target, DamageSpecifier damage, FixedPoint2 damageRequired)
    {
        var comp = projectile.Comp;
        // <Goob> - Splits penetration change if target have PenetratableComponent
        if (TryComp<PenetratableComponent>(target, out var penetratable))
        {
            // Here penetration threshold count as "penetration health".
            // If it's lower than damage than penetation damage entity cause it deletes projectile
            if (comp.PenetrationThreshold < penetratable.PenetrateDamage)
                return false;

            comp.PenetrationThreshold -= FixedPoint2.New(penetratable.PenetrateDamage);
            comp.Damage *= (1 - penetratable.DamagePenaltyModifier);
            return true;
        }
        // </Goob>

        // If penetration is to be considered, we need to do some checks to see if the projectile should stop.
        if (comp.PenetrationThreshold == 0)
            return false;
        // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
        if (comp.PenetrationDamageTypeRequirement != null)
        {
            foreach (var requiredDamageType in comp.PenetrationDamageTypeRequirement)
            {
                if (!damage.DamageDict.Keys.Contains(requiredDamageType))
                    return false;
            }
        }

        // If the object won't be destroyed, it "tanks" the penetration hit.
        if (damage.GetTotal() < damageRequired)
        {
            return false;
        }

        if (!comp.ProjectileSpent)
        {
            comp.PenetrationAmount += damageRequired;
            // The projectile has dealt enough damage to be spent.
            if (comp.PenetrationAmount >= comp.PenetrationThreshold)
            {
                return false;
            }
        }

        return true;
    }
}
