using Content.Goobstation.Common.Projectiles;
using Content.Shared._Shitmed.Body;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Goob - API methods for gun targeting
/// </summary>
public abstract partial class SharedGunSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private HashSet<Entity<BodyComponent>> _bodies = new();

    public TargetBodyPart? GetTargetPart(Entity<TargetingComponent?>? targeting,
        MapCoordinates shootCoords,
        MapCoordinates targetCoords)
    {
        if (shootCoords.MapId != targetCoords.MapId || targeting is not {} ent)
            return null;

        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        var dist = (shootCoords.Position - targetCoords.Position).Length();
        var missChance = MathHelper.Lerp(0f, 1f, Math.Clamp(dist / 2f, 0f, 1f));
        var seed = SharedRandomExtensions.HashCodeCombine((int) Timing.CurTick.Value, GetNetEntity(ent).Id);
        var random = new System.Random(seed);
        return random.Prob(missChance) ? TargetBodyPart.Chest : ent.Comp.Target;
    }

    public void SetProjectilePerfectHitEntities(EntityUid projectile,
        Entity<TargetingComponent?>? shooter,
        MapCoordinates coords)
    {
        if (shooter is not {} ent)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var comp = EnsureComp<ProjectileMissTargetPartChanceComponent>(projectile);
        _bodies.Clear();
        _lookup.GetEntitiesInRange<BodyComponent>(coords, 2f, _bodies, LookupFlags.Dynamic);
        foreach (var (uid, body) in _bodies)
        {
            if (body.BodyType != BodyType.Complex)
                continue;

            var part = GetTargetPart(shooter, coords, TransformSystem.GetMapCoordinates(ent));

            if (part is null or TargetBodyPart.Chest)
                continue;

            comp.PerfectHitEntities.Add(uid);
            Dirty(projectile, comp);
        }
    }
}
