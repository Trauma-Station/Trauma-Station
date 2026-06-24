// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Trauma.Shared.Actions;

// not to be confused with ActionGunSystem
public sealed partial class GunActionSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunActionComponent, ActionGunShootEvent>(OnShoot);
    }

    private void OnShoot(Entity<GunActionComponent> ent, ref ActionGunShootEvent args)
    {
        var user = args.Performer;
        var gun = Comp<GunComponent>(ent);
        args.Handled = _gun.AttemptShoot(user, (ent, gun), args.Target, args.Entity);
    }
}
