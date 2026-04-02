// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Weapons.Ranged;
using Content.Lavaland.Shared.Pressure;
using Content.Lavaland.Shared.Weapons.Upgrades;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;

namespace Content.Lavaland.Server.Pressure;

public sealed class PressureEfficiencyChangeSystem : SharedPressureEfficiencyChangeSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    private EntityQuery<PressureDamageChangeComponent> _query;
    private EntityQuery<ProjectileComponent> _projectileQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<PressureDamageChangeComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<PressureDamageChangeComponent, GetMeleeDamageEvent>(OnGetDamage,
            after: [ typeof(GunUpgradeSystem), typeof(SharedWieldableSystem) ]);
        SubscribeLocalEvent<PressureDamageChangeComponent, ProjectileShotEvent>(OnProjectileShot,
            after: [ typeof(GunUpgradeSystem) ]); // let this system reduce damage upgrades' added damage automatically

        SubscribeLocalEvent<PressureArmorChangeComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnArmorRelayDamageModify, before: [typeof(SharedArmorSystem)]);
    }

    private void OnGetDamage(Entity<PressureDamageChangeComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (ent.Comp.ApplyToMelee && ApplyModifier(ent.AsNullable()))
            args.Damage *= ent.Comp.AppliedModifier;
    }

    private void OnProjectileShot(Entity<PressureDamageChangeComponent> ent, ref ProjectileShotEvent args)
    {
        if (!ApplyModifier(ent.AsNullable())
            || !ent.Comp.ApplyToProjectiles
            || !_projectileQuery.TryComp(args.FiredProjectile, out var projectile))
            return;

        projectile.Damage *= ent.Comp.AppliedModifier;
    }

    public bool ApplyModifier(Entity<PressureDamageChangeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var pressure = _atmos.GetTileMixture((ent.Owner, Transform(ent)))?.Pressure ?? 0f;
        return ent.Comp.Enabled && ((pressure >= ent.Comp.LowerBound
            && pressure <= ent.Comp.UpperBound) == ent.Comp.ApplyWhenInRange);
    }

    /// <summary>
    /// Get the damage modifier for a weapon, returning 1 if it doesn't have the component.
    /// </summary>
    public float GetModifier(Entity<PressureDamageChangeComponent?> ent)
        => _query.Resolve(ent, ref ent.Comp, false) ? ent.Comp.AppliedModifier : 1f;

    private void OnArmorRelayDamageModify(Entity<PressureArmorChangeComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!ApplyModifier(ent.Owner) ||
            args.Args.TargetPart is not {} part ||
            !TryComp<ArmorComponent>(ent, out var armor))
            return;

        var coverage = armor.ArmorCoverage;
        if (!coverage.Contains(part))
            return;

        args.Args.Damage.ArmorPenetration += ent.Comp.ExtraPenetrationModifier;
    }
}
