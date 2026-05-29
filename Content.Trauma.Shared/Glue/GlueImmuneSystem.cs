// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Trauma.Common.Glue;

namespace Content.Trauma.Shared.Glue;

public sealed class GlueImmuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<GlueImmuneComponent, GluedPickUpAttemptEvent>(OnPickUpAttempt);
    }

    private void OnPickUpAttempt(Entity<GlueImmuneComponent> ent, ref GluedPickUpAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
