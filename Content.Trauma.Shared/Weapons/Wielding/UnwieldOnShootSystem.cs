// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Trauma.Shared.Weapons.Wielding;

public sealed partial class UnwieldOnShootSystem : EntitySystem
{
    [Dependency] private SharedWieldableSystem _wieldable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnwieldOnShootComponent, GunShotEvent>(OnShoot);
    }

    private void OnShoot(EntityUid uid, UnwieldOnShootComponent component, ref GunShotEvent args)
    {
        if (TryComp(uid, out WieldableComponent? wieldable))
            _wieldable.TryUnwield(uid, wieldable, args.User);
    }
}
