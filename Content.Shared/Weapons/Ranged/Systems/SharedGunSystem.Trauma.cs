using System.Numerics;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Components;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Trauma - methods moved out of server
/// </summary>
public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;

    private static readonly EntProtoId ShootingKnowledge = "ShootingKnowledge";
    private static readonly EntProtoId WeaponsKnowledge = "WeaponsKnowledge";

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
    /// Trauma - changed component to Entity, added user, made public
    /// </summary>
    public Angle GetRecoilAngle(TimeSpan curTime, Entity<GunComponent> ent, Angle direction, EntityUid? user = null, float spreadScale = 1.0f)
    {
        var (uid, comp) = ent;
        var timeSinceLastFire = (curTime - comp.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(comp.CurrentAngle.Theta + spreadScale * comp.AngleIncreaseModified.Theta - comp.AngleDecayModified.Theta * timeSinceLastFire, comp.MinAngleModified.Theta + 0.05f * Math.Max(spreadScale - 1.0f, 0), comp.MaxAngleModified.Theta);
        comp.CurrentAngle = new Angle(newTheta);
        comp.LastFire = comp.NextFire;

        // Convert it so angle can go either side.
        var random = Random(uid).NextFloat(-0.5f, 0.5f);

        // <Goob>
        var angleEv = new GetRecoilModifiersEvent(uid, user ?? uid);
        if (user != null)
            RaiseLocalEvent(user.Value, ref angleEv);
        RaiseLocalEvent(uid, ref angleEv);
        random *= angleEv.Modifier;
        // </Goob>

        var spread = comp.CurrentAngle.Theta * random * spreadScale;
        var angle = new Angle(direction.Theta + comp.CurrentAngle.Theta * random * spreadScale);
        //DebugTools.Assert(spread <= comp.MaxAngleModified.Theta * spreadScale || spread <= comp.MinAngleModified.Theta + 0.05f * Math.Max(spreadScale - 1.0f, 0));
        return angle;
    }

    /// <summary>
    /// Gets recoil scale for gun according to knowledge system.
    /// </summary>
    private float GetRecoilScale(EntityUid? userUid, EntityUid gun)
    {
        if (userUid is not {} user || !HasComp<KnowledgeHolderComponent>(user))
            return 1;

        if (_knowledge.GetKnowledge(user, ShootingKnowledge) is not {} shooting)
            return 3;

        return shooting.Comp.Level < 26
            ? 3.0f - (float) shooting.Comp.Level / 26.0f - _knowledge.SharpCurve(shooting)
            : 1.0f - ((float) (shooting.Comp.Level - 50) / 50.0f * (float) (shooting.Comp.Level - 50) / 50.0f);
    }

    /// <summary>
    /// Adds shooting experience according to knowledge system.
    /// </summary>
    private void AddShootingExperience(EntityUid? userUid)
    {
        if (userUid is not {} user)
            return;

        var evShooting = new AddExperienceEvent(ShootingKnowledge, 1);
        var evWeapons = new AddExperienceEvent(WeaponsKnowledge, 1);
        RaiseLocalEvent(user, ref evShooting);
        RaiseLocalEvent(user, ref evWeapons);
    }

    /// <summary>
    /// Attempts to copy knowledge construction modifiers from the specified ammo entity to the target entity. If the
    /// ammo entity contains knowledge modifier data, it is applied to the target entity.
    /// </summary>
    /// <remarks>If the ammo entity does not have a QualityComponent, no modifiers are
    /// added to the target entity.</remarks>
    /// <param name="ammoEnt">The entity identifier of the ammo from which knowledge construction modifiers are sourced.</param>
    /// <param name="newUid">The entity identifier of the target to which knowledge construction modifiers will be added.</param>
    private void TryAddKnowledgeModifiers(EntityUid? ammoEnt, EntityUid newUid)
    {
        // TODO: move this out of here bruh
        if (!TryComp<QualityComponent>(ammoEnt, out var ammoKnowledge))
            return;

        var newKnowledge = EnsureComp<QualityComponent>(newUid);
        newKnowledge.LevelDeltas = new Dictionary<EntProtoId, int>(ammoKnowledge.LevelDeltas);
        newKnowledge.Quality = ammoKnowledge.Quality;
        newKnowledge.NumberOfMasteries = ammoKnowledge.NumberOfMasteries;

        _knowledge.ModifyValues((newUid, newKnowledge));
    }
}
