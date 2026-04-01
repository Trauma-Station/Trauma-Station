// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable.Components;
using Content.Trauma.Shared.Wizard.Projectiles;

namespace Content.Trauma.Shared.Weapons.SmartGun;

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
            GetEntity(targeted.Target) is not { } target || target == Transform(uid).ParentUid)
            return;

        if (comp.RequiresWield && !(TryComp(uid, out WieldableComponent? wieldable) && wieldable.Wielded))
            return;

        homing.Target = target;
        Dirty(projectile, homing);
    }
}
