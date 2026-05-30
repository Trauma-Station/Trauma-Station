// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class ProjectileModifyStatusEffectSystem : EntitySystem
{
    [Dependency] private EntityQuery<ProjectileComponent> _projQuery = default!;
    [Dependency] private EntityQuery<ReflectiveComponent> _reflectiveQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileModifyStatusEffectComponent, StatusEffectRelayedEvent<AmmoShotUserEvent>>(OnAmmoShot);
    }

    private void OnAmmoShot(Entity<ProjectileModifyStatusEffectComponent> ent, ref StatusEffectRelayedEvent<AmmoShotUserEvent> args)
    {
        var ev = args.Args;

        var projectiles = ev.FiredProjectiles;
        if (projectiles is not { } projList || projList.Count == 0)
            return;

        var modifier = ent.Comp.Modifier;
        foreach (var projectile in projList)
        {
            if (!_projQuery.TryComp(projectile, out var projComp))
                continue;

            if (ent.Comp.Laser && !_reflectiveQuery.HasComp(projectile))
                continue;

            projComp.Damage *= modifier;
            Dirty(projectile, projComp);
        }
    }
}
