// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Spawns an ent on startup and stores its uid.
/// </summary>
public sealed partial class LinkedEntityContainerSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Startup(Entity<LinkedEntityContainerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Uid = PredictedSpawnNextToOrDrop(ent.Comp.EntityToSpawn, ent.Owner);
        Dirty(ent.Owner, ent.Comp);
    }
}
