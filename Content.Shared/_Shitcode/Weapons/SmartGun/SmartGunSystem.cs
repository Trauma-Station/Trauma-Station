// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Wizard.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Content.Lavaland.Common.Weapons.Ranged;

namespace Content.Shared._Goobstation.Weapons.SmartGun;

public sealed class SmartGunSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmartGunComponent, ProjectileShotEvent>(OnShot);
    }

    private void OnShot(Entity<SmartGunComponent> ent, ref ProjectileShotEvent args)
    {
        var (uid, comp) = ent;

        var projectile = args.FiredProjectile;

        if (!TryComp(projectile, out HomingProjectileComponent? homing) ||
            !TryComp(projectile, out TargetedProjectileComponent? targeted) ||
            targeted.Target is not { } target || target == Transform(uid).ParentUid)
            return;

        if (comp.RequiresWield && !(TryComp(uid, out WieldableComponent? wieldable) && wieldable.Wielded))
            return;

        homing.Target = target;
        Dirty(projectile, homing);
    }
}
