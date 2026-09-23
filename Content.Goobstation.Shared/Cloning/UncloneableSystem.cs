// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cloning.Events;

namespace Content.Goobstation.Shared.Cloning;

public sealed partial class UncloneableSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnCloningAttempt(Entity<UncloneableComponent> ent, ref CloningAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
