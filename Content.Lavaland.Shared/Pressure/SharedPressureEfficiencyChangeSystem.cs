// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Weapons.Ranged;
using Content.Lavaland.Shared.Weapons.Upgrades;
using Content.Shared.Armor;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;

namespace Content.Lavaland.Shared.Pressure;

public abstract partial class SharedPressureEfficiencyChangeSystem : EntitySystem
{
    [Dependency] private EntityQuery<ArmorComponent> _armorQuery = default!;
    [Dependency] private EntityQuery<PressureEfficiencyComponent> _query = default!;
    [Dependency] private EntityQuery<ProjectileComponent> _projectileQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressureDamageChangeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PressureArmorChangeComponent, ExaminedEvent>(OnArmorExamined);

        SubscribeLocalEvent<PressureDamageChangeComponent, GetMeleeDamageEvent>(OnGetDamage,
            after: [ typeof(GunUpgradeSystem), typeof(SharedWieldableSystem) ]);
        SubscribeLocalEvent<PressureDamageChangeComponent, ProjectileShotEvent>(OnProjectileShot,
            after: [ typeof(GunUpgradeSystem) ]); // let this system reduce damage upgrades' added damage automatically
        SubscribeLocalEvent<PressureArmorChangeComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnArmorRelayDamageModify,
            before: [typeof(SharedArmorSystem)]);
    }

    private void OnExamined(Entity<PressureDamageChangeComponent> ent, ref ExaminedEvent args)
    {
        var localeKey = "lavaland-examine-pressure-";

        ExamineHelper(ent.Owner,
            Math.Round(ent.Comp.AppliedModifier, 2),
            localeKey,
            ref args);
    }

    private void OnArmorExamined(Entity<PressureArmorChangeComponent> ent, ref ExaminedEvent args)
    {
        var localeKey = "lavaland-examine-pressure-armor-";
        ExamineHelper(ent.Owner,
            Math.Round(ent.Comp.ExtraPenetrationModifier * 100),
            localeKey,
            ref args);
    }

    private void ExamineHelper(Entity<PressureEfficiencyComponent?> ent, double modifier, string localeKey, ref ExaminedEvent args)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return;

        localeKey += ent.Comp.ApplyWhenInRange ? "in-range-" : "out-range-";
        localeKey += modifier < 1f ? "debuff" : "buff";

        modifier = Math.Abs(modifier);
        var min = Math.Round(ent.Comp.LowerBound);
        var max = Math.Round(ent.Comp.UpperBound);
        args.PushMarkup(Loc.GetString(localeKey, ("min", min), ("max", max), ("modifier", modifier)));
    }

    private void OnGetDamage(Entity<PressureDamageChangeComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (ent.Comp.ApplyToMelee && ApplyModifier(ent.Owner, args.User))
            args.Damage *= ent.Comp.AppliedModifier;
    }

    private void OnProjectileShot(Entity<PressureDamageChangeComponent> ent, ref ProjectileShotEvent args)
    {
        if (!ApplyModifier(ent.Owner, args.User ?? ent)
            || !ent.Comp.ApplyToProjectiles
            || !_projectileQuery.TryComp(args.FiredProjectile, out var projectile))
            return;

        projectile.Damage *= ent.Comp.AppliedModifier;
        Dirty(args.FiredProjectile, projectile);
    }

    public bool ApplyModifier(Entity<PressureEfficiencyComponent?> ent, EntityUid user)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        var pressure = EnsureComp<PressureTrackerComponent>(user).Pressure;
        var inRange = pressure >= ent.Comp.LowerBound && pressure <= ent.Comp.UpperBound;
        return ent.Comp.Enabled && (inRange == ent.Comp.ApplyWhenInRange);
    }

    private void OnArmorRelayDamageModify(Entity<PressureArmorChangeComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (args.Args.TargetPart is not {} part ||
            ApplyModifier(ent.Owner, args.Owner) || // inverted compared to damage as armor should be weaker on station, not on lavaland
            !_armorQuery.TryComp(ent, out var armor))
            return;

        var coverage = armor.ArmorCoverage;
        if (!coverage.Contains(part))
            return;

        args.Args.Damage.ArmorPenetration += ent.Comp.ExtraPenetrationModifier;
    }
}
