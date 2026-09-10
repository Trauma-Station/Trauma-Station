// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Trauma.Shared.Weapons.UseDelay;

public sealed partial class UseDelayBlockShootSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _useDelay = default!;

    [SubscribeLocalEvent]
    private void OnShootAttempt(Entity<UseDelayBlockShootComponent> ent, ref AttemptShootEvent args)
    {
        if (TryComp(ent, out UseDelayComponent? useDelay) && _useDelay.IsDelayed((ent, useDelay)))
            args.Cancelled = true;
    }
}
