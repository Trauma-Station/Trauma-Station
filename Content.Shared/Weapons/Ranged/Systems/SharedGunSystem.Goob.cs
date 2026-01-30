using Content.Goobstation.Common.Projectiles;
using Content.Shared._Shitmed.Body;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Goob - API methods for gun targeting and AP stuff
/// </summary>
public abstract partial class SharedGunSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private HashSet<Entity<BodyComponent>> _bodies = new();

    private void InitializeGoob()
    {
        InitializeBasicHitScan(); // wizard shitcode :(

        SubscribeLocalEvent<BasicEntityAmmoProviderComponent, DamageExamineEvent>(OnBasicEntityDamageExamine);
    }

    private void OnBasicEntityDamageExamine(Entity<BasicEntityAmmoProviderComponent> ent, ref DamageExamineEvent args)
    {
        if (ent.Comp.Proto is not {} proto || GetProjectileDamage(proto) is not {} damage)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damage), Loc.GetString("damage-projectile"));

        var ap = GetProjectilePenetration(proto);
        if (ap == 0)
            return;

        var abs = Math.Abs(ap);
        args.Message.AddMarkupPermissive("\n" + Loc.GetString("armor-penetration", ("arg", ap/abs), ("abs", abs)));
    }

    public TargetBodyPart? GetTargetPart(EntityUid? shooter, EntityUid target)
        => shooter is {} targeting
            ? GetTargetPart(targeting, TransformSystem.GetMapCoordinates(targeting), TransformSystem.GetMapCoordinates(target))
            : null;

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

    /// <summary>
    /// Get armor penetration for a projectile or hitscan prototype, from 0-100.
    /// </summary>
    public int GetProjectilePenetration(EntProtoId id)
    {
        if (!ProtoManager.Resolve(id, out var proto))
            return 0;

        // goida
        if (proto.TryGetComponent<ProjectileComponent>(out var p, Factory))
            return p.IgnoreResistances ? 100 : (int)Math.Round(p.Damage.ArmorPenetration * 100);
        if (proto.TryGetComponent<HitscanBasicDamageComponent>(out var hitscan, Factory))
            return (int)Math.Round(hitscan.Damage.ArmorPenetration * 100);
        return 0;
    }
}
