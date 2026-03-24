// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared.Flash;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeRust()
    {
        base.SubscribeRust();

        SubscribeLocalEvent<Shared.Heretic.Components.PathSpecific.Rust.RustbringerComponent, FlashAttemptEvent>(OnFlashAttempt);
    }

    private void OnFlashAttempt(Entity<Shared.Heretic.Components.PathSpecific.Rust.RustbringerComponent> ent, ref FlashAttemptEvent args)
    {
        if (!IsTileRust(Transform(ent).Coordinates, out _))
            return;

        args.Cancelled = true;
    }
}
