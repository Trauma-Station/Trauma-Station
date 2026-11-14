// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Despawns linked ent on shutdown.
/// </summary>
public sealed partial class LinkedEntityContainerSystem : EntitySystem
{
    [SubscribeLocalEvent]
    public void OnShutdown(Entity<LinkedEntityContainerComponent> ent, ref ComponentShutdown args)
    {
        PredictedDel(ent.Comp.Uid);
    }
}
