// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Trauma.Common.Lube;

namespace Content.Trauma.Shared.Lube;

public sealed class LubeImmuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<LubeImmuneComponent, LubedPickUpAttemptEvent>(OnPickUpAttempt);
    }

    private void OnPickUpAttempt(Entity<LubeImmuneComponent> ent, ref LubedPickUpAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
