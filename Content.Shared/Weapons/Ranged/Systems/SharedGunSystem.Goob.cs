using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Goob - API methods for gun targeting and AP stuff
/// </summary>
public abstract partial class SharedGunSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;

    private HashSet<Entity<BodyComponent>> _bodies = new();

    private void InitializeGoob()
    {
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
