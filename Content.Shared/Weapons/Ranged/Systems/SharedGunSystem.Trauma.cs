// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Inventory;
using Content.Shared.Power;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Trauma - methods moved out of server
/// </summary>
public abstract partial class SharedGunSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedMapSystem _map = default!;

    /// <summary>
    /// Get a predicted random instance for an entity, specific to this tick.
    /// </summary>
    public System.Random Random(EntityUid uid)
        => SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(uid));

    /// <summary>
    /// Client-overriden function to do recoil for a shot.
    /// Shooting is fully predicted so server doesn't need to do anything.
    /// </summary>
    protected virtual void Recoil(EntityUid? user, Vector2 recoil, float recoilScalar)
    {
    }

    private void ShootOrThrow(EntityUid uid, Vector2 mapDirection, Vector2 gunVelocity, Entity<GunComponent> gun, EntityUid? user,
        Vector2? targetCoordinates = null)
    {
        if (gun.Comp.Target is { } target && !TerminatingOrDeleted(target))
        {
            var targeted = EnsureComp<TargetedProjectileComponent>(uid);
            targeted.Target = GetNetEntity(target);
            Dirty(uid, targeted);
        }

        // Do a throw
        if (!HasComp<ProjectileComponent>(uid))
        {
            RemoveShootable(uid);
            // TODO: Someone can probably yeet this a billion miles so need to pre-validate input somewhere up the call stack.
            ThrowingSystem.TryThrow(uid, mapDirection, gun.Comp.ProjectileSpeedModified, user);
            return;
        }

        ShootProjectile(uid, mapDirection, gunVelocity, gun, user, gun.Comp.ProjectileSpeedModified,
            targetCoordinates);
    }

    /// <summary>
    /// Gets a linear spread of angles between start and end.
    /// </summary>
    /// <param name="start">Start angle in degrees</param>
    /// <param name="end">End angle in degrees</param>
    /// <param name="intervals">How many shots there are</param>
    public Angle[] LinearSpread(Angle start, Angle end, int intervals)
    {
        var angles = new Angle[intervals];
        DebugTools.Assert(intervals > 1);

        for (var i = 0; i <= intervals - 1; i++)
        {
            angles[i] = new Angle(start + (end - start) * i / (intervals - 1));
        }

        return angles;
    }

    /// <summary>
    /// Trauma - changed component to Entity, added user, made public
    /// </summary>
    public Angle GetRecoilAngle(TimeSpan curTime, Entity<GunComponent> ent, Angle direction, EntityUid? user = null)
    {
        var (uid, comp) = ent;
        var timeSinceLastFire = (curTime - comp.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(comp.CurrentAngle.Theta + comp.AngleIncreaseModified.Theta - comp.AngleDecayModified.Theta * timeSinceLastFire, comp.MinAngleModified.Theta, comp.MaxAngleModified.Theta);
        comp.CurrentAngle = new Angle(newTheta);
        comp.LastFire = comp.NextFire;

        // Convert it so angle can go either side.
        var random = Random(uid).NextFloat(-0.5f, 0.5f);

        var angleEv = new GetRecoilModifiersEvent(uid, user ?? uid);
        if (user != null)
            RaiseLocalEvent(user.Value, ref angleEv);
        RaiseLocalEvent(uid, ref angleEv);
        random *= angleEv.Modifier;

        var spread = comp.CurrentAngle.Theta * random;
        var angle = new Angle(direction.Theta + comp.CurrentAngle.Theta * random);
        DebugTools.Assert(spread <= comp.MaxAngleModified.Theta * angleEv.Modifier);
        return angle;
    }

    public (float, float) GetBatteryShotsFloat(Entity<BatteryAmmoProviderComponent> ent)
    {
        var ev = new GetChargeEvent();
        RaiseLocalEvent(ent, ref ev);
        var currentShots = ev.CurrentCharge / ent.Comp.FireCost;
        var maxShots = ev.MaxCharge / ent.Comp.FireCost;

        return (currentShots, maxShots);
    }
}
