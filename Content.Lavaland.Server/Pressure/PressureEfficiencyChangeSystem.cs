// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aineias1 <dmitri.s.kiselev@gmail.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 McBosserson <148172569+McBosserson@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Milon <plmilonpl@gmail.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Rouden <149893554+Roudenn@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Unlumination <144041835+Unlumy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Weapons.Ranged;
using Content.Lavaland.Shared.Pressure;
using Content.Lavaland.Shared.Weapons.Upgrades;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Armor;
using Content.Shared.Body.Systems;
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
    [Dependency] private readonly SharedBodySystem _body = default!;

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
        if (!ApplyModifier(ent)
            || !ent.Comp.ApplyToMelee)
            return;

        args.Damage *= ent.Comp.AppliedModifier;
    }

    private void OnProjectileShot(Entity<PressureDamageChangeComponent> ent, ref ProjectileShotEvent args)
    {
        if (!ApplyModifier(ent)
            || !ent.Comp.ApplyToProjectiles
            || !_projectileQuery.TryComp(args.FiredProjectile, out var projectile))
            return;

        projectile.Damage *= ent.Comp.AppliedModifier;
    }

    public bool ApplyModifier(Entity<PressureDamageChangeComponent> ent)
    {
        var pressure = _atmos.GetTileMixture((ent.Owner, Transform(ent)))?.Pressure ?? 0f;
        return ent.Comp.Enabled && ((pressure >= ent.Comp.LowerBound
            && pressure <= ent.Comp.UpperBound) == ent.Comp.ApplyWhenInRange);
    }

    /// <summary>
    /// Get the damage modifier for a weapon, returning 1 if it doesn't have the component.
    /// </summary>
    public float GetModifier(Entity<PressureDamageChangeComponent?> ent)
        => _query.Resolve(ent, ref ent.Comp) ? ent.Comp.AppliedModifier : 1f;

    private void OnArmorRelayDamageModify(Entity<PressureArmorChangeComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        var pressure = _atmos.GetTileMixture((ent.Owner, Transform(ent)))?.Pressure ?? 0f;
        if ((pressure >= ent.Comp.LowerBound && pressure <= ent.Comp.UpperBound) != ent.Comp.ApplyWhenInRange
            || args.Args.TargetPart == null
            || !TryComp<ArmorComponent>(ent, out var armor))
            return;

        var (partType, _) = _body.ConvertTargetBodyPart(args.Args.TargetPart); // Woundmed stuff
        var coverage = armor.ArmorCoverage;
        if (!coverage.Contains(partType))
            return;

        args.Args.Damage.ArmorPenetration += ent.Comp.ExtraPenetrationModifier;
    }
}
