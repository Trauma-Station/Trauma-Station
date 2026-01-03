using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Trauma - methods moved out of server
/// </summary>
public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <summary>
    /// Get a predicted random instance for an entity, specific to this tick.
    /// </summary>
    private System.Random Random(EntityUid uid)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int) Timing.CurTick.Value, GetNetEntity(uid).Id);
        return new System.Random(seed);
    }

    /// <summary>
    /// Client-overriden function to do recoil for a shot.
    /// Shooting is fully predicted so server doesn't need to do anything.
    /// </summary>
    protected virtual void Recoil(EntityUid? user, Vector2 recoil, float recoilScalar)
    {
    }

    private void ShootOrThrow(EntityUid uid, Vector2 mapDirection, Vector2 gunVelocity, GunComponent gun, EntityUid gunUid, EntityUid? user, Vector2? targetCoordinates = null) // Goobstation
    {
        if (gun.Target is { } target && !TerminatingOrDeleted(target))
        {
            var targeted = EnsureComp<TargetedProjectileComponent>(uid);
            targeted.Target = target;
            Dirty(uid, targeted);
        }

        // Do a throw
        if (!HasComp<ProjectileComponent>(uid))
        {
            RemoveShootable(uid);
            // TODO: Someone can probably yeet this a billion miles so need to pre-validate input somewhere up the call stack.
            ThrowingSystem.TryThrow(uid, mapDirection, gun.ProjectileSpeedModified, user);
            return;
        }

        ShootProjectile(uid, mapDirection, gunVelocity, gunUid, user, gun.ProjectileSpeedModified, targetCoordinates); // Goobstation
    }

    /// <summary>
    /// Gets a linear spread of angles between start and end.
    /// </summary>
    /// <param name="start">Start angle in degrees</param>
    /// <param name="end">End angle in degrees</param>
    /// <param name="intervals">How many shots there are</param>
    public Angle[] LinearSpread(Angle start, Angle end, int intervals) // Goob edit
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
    /// Trauma - changed component to Entity, added user
    /// </summary>
    private Angle GetRecoilAngle(TimeSpan curTime, Entity<GunComponent> ent, Angle direction, EntityUid? user = null)
    {
        var (uid, comp) = ent;
        var timeSinceLastFire = (curTime - comp.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(comp.CurrentAngle.Theta + comp.AngleIncreaseModified.Theta - comp.AngleDecayModified.Theta * timeSinceLastFire, comp.MinAngleModified.Theta, comp.MaxAngleModified.Theta);
        comp.CurrentAngle = new Angle(newTheta);
        comp.LastFire = comp.NextFire;

        // Convert it so angle can go either side.
        var random = Random(uid).NextFloat(-0.5f, 0.5f);

        // <Goob>
        var angleEv = new GetRecoilModifiersEvent(uid, user ?? uid);
        if (user != null)
            RaiseLocalEvent(user.Value, ref angleEv);
        RaiseLocalEvent(comp.Owner, ref angleEv);
        random *= angleEv.Modifier;
        // </Goob>

        var spread = comp.CurrentAngle.Theta * random;
        var angle = new Angle(direction.Theta + comp.CurrentAngle.Theta * random);
        DebugTools.Assert(spread <= comp.MaxAngleModified.Theta);
        return angle;
    }
}
