// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons;
using Content.Lavaland.Common.Weapons;
using Content.Lavaland.Common.Weapons.Ranged;
using Content.Lavaland.Shared.Pressure;
using Content.Lavaland.Shared.Weapons.Upgrades.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Weapons.Ranged;
using Robust.Shared.Containers;

namespace Content.Lavaland.Shared.Weapons.Upgrades;

public sealed partial class GunUpgradeSystem
{
    [Dependency] private EntityQuery<ProjectileComponent> _projQuery = default!;

    [SubscribeLocalEvent]
    private void OnFireRateRefresh(Entity<GunUpgradeFireRateComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.FireRate *= ent.Comp.Coefficient;
        args.BurstFireRate *= ent.Comp.Coefficient;
        args.BurstCooldown /= ent.Comp.Coefficient;
    }

    [SubscribeLocalEvent]
    private void OnFireRateRefreshRecharge(Entity<GunUpgradeFireRateComponent> ent, ref RechargeBasicEntityAmmoGetCooldownModifiersEvent args)
    {
        args.Multiplier /= ent.Comp.Coefficient;
    }

    [SubscribeLocalEvent]
    private void OnCompsUpgradeInsert(Entity<GunUpgradeComponentsComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_timing.ApplyingState && HasComp<UpgradeableWeaponComponent>(args.Container.Owner))
            EntityManager.AddComponents(args.Container.Owner, ent.Comp.Components);
    }

    [SubscribeLocalEvent]
    private void OnCompsUpgradeEject(Entity<GunUpgradeComponentsComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!_timing.ApplyingState && HasComp<UpgradeableWeaponComponent>(args.Container.Owner))
            EntityManager.RemoveComponents(args.Container.Owner, ent.Comp.Components);
    }

    [SubscribeLocalEvent]
    private void OnSpeedRefresh(Entity<GunUpgradeSpeedComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.ProjectileSpeed *= ent.Comp.Coefficient;
    }

    [SubscribeLocalEvent]
    private void OnDamageGunShotComps(Entity<GunUpgradeProjectileComponentsComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (_projQuery.HasComp(ammo))
                EntityManager.AddComponents(ammo.Value, ent.Comp.Components);
        }
    }

    [SubscribeLocalEvent]
    private void OnVampirismGunShot(Entity<GunUpgradeVampirismComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (!_projQuery.HasComp(ammo))
                continue;

            var comp = EnsureComp<ProjectileVampirismComponent>(ammo.Value);
            comp.DamageOnHit = ent.Comp.DamageOnHit;
        }
    }

    [SubscribeLocalEvent]
    private void OnVampirismProjectileHit(Entity<ProjectileVampirismComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !HasComp<MobStateComponent>(args.Target))
            return;

        _damage.ChangeDamage(shooter, ent.Comp.DamageOnHit, ignoreResistances: true);
    }

    [SubscribeLocalEvent]
    private void OnGetMeleeRelay(Entity<GunUpgradeBayonetComponent> ent, ref GetRelayMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        args.Found = ent.Owner;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDamageShot(Entity<GunUpgradeDamageComponent> ent, ref GunShotProjectileEvent args)
    {
        if (!_projQuery.TryComp(args.FiredProjectile, out var projectile))
            return;

        if (ent.Comp.BonusDamage is { } bonus)
            projectile.Damage += bonus;
        projectile.Damage *= ent.Comp.Modifier;
        Dirty(args.FiredProjectile, projectile);
    }

    [SubscribeLocalEvent]
    private void OnPressureInsert(Entity<GunUpgradePressureComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var weapon = args.Container.Owner;
        var comp = ent.Comp;
        if (!TryComp<PressureEfficiencyComponent>(weapon, out var pec) ||
            !TryComp<PressureDamageChangeComponent>(weapon, out var pdc))
            return;

        comp.SavedAppliedModifier = pdc.AppliedModifier;
        comp.SavedApplyWhenInRange = pec.ApplyWhenInRange;
        comp.SavedLowerBound = pec.LowerBound;
        comp.SavedUpperBound = pec.UpperBound;

        if (comp.NewAppliedModifier is { } newModifier)
        {
            pdc.AppliedModifier = newModifier;
            Dirty(weapon, pdc);
        }
        if (comp.NewApplyWhenInRange is { } newApplyInRange)
            pec.ApplyWhenInRange = newApplyInRange;
        if (comp.NewLowerBound is { } newLower)
            pec.LowerBound = newLower;
        if (comp.NewUpperBound is { } newUpper)
            pec.UpperBound = newUpper;
        Dirty(weapon, pec);
    }

    [SubscribeLocalEvent]
    private void OnPressureEject(Entity<GunUpgradePressureComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var weapon = args.Container.Owner;
        var comp = ent.Comp;
        if (!TryComp<PressureEfficiencyComponent>(weapon, out var pec))
            return;

        if (TryComp<PressureDamageChangeComponent>(weapon, out var pdc))
        {
            pdc.AppliedModifier = comp.SavedAppliedModifier;
            Dirty(weapon, pdc);
        }
        pec.ApplyWhenInRange = comp.SavedApplyWhenInRange;
        pec.LowerBound = comp.SavedLowerBound;
        pec.UpperBound = comp.SavedUpperBound;
        Dirty(weapon, pec);
    }

    [SubscribeLocalEvent]
    private void OnEffectsUpgradeHit(Entity<WeaponUpgradeEffectsComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var hit in args.HitEntities)
        {
            _effects.ApplyEffects(hit, ent.Comp.Effects);
        }
    }

    /* Melee */

    [SubscribeLocalEvent]
    private void OnGetMeleeDamage(Entity<WeaponUpgradeDamageComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (ent.Comp.BonusDamage != null)
            args.Damage += ent.Comp.BonusDamage;
        args.Damage *= ent.Comp.Modifier;
    }

    [SubscribeLocalEvent]
    private void OnGetRange(Entity<WeaponUpgradeRangeComponent> ent, ref GetLightAttackRangeEvent args)
    {
        if (ent.Comp.BonusRange != null)
            args.Range += ent.Comp.BonusRange.Value;
        if (ent.Comp.RangeMultiplier != null)
            args.Range *= ent.Comp.RangeMultiplier.Value;
    }

    [SubscribeLocalEvent]
    private void OnGetAttackRate(Entity<WeaponUpgradeSpeedComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (ent.Comp.BonusAttackRate != null)
            args.Rate += ent.Comp.BonusAttackRate.Value;
        if (ent.Comp.AttackRateMultiplier != null)
            args.Multipliers *= ent.Comp.AttackRateMultiplier.Value;
    }
}
