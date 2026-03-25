// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Flash;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeRust()
    {
        base.SubscribeRust();

        SubscribeLocalEvent<RustbringerComponent, FlashAttemptEvent>(OnFlashAttempt);
    }

    private void OnFlashAttempt(Entity<RustbringerComponent> ent, ref FlashAttemptEvent args)
    {
        if (!IsTileRust(Transform(ent).Coordinates, out _))
            return;

        args.Cancelled = true;
    }
}
