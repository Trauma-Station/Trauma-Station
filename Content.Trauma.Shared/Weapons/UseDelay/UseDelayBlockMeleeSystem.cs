// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Weapons.UseDelay;

public sealed partial class UseDelayBlockMeleeSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _useDelay = default!;

    [SubscribeLocalEvent]
    private void OnMeleeAttempt(Entity<UseDelayBlockMeleeComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!TryComp(ent, out UseDelayComponent? useDelay))
            return;

        if (ent.Comp.Delays.Any(delay => _useDelay.IsDelayed((ent, useDelay), delay)))
            args.Cancelled = true;
    }
}
