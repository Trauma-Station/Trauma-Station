// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Teleportation.Components;

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Spawns an ent on startup and stores its uid.
/// </summary>
public sealed partial class LinkedEntityContainerSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    private EntProtoId<PortalComponent> _portalDestination = "BluespaceRiftPortalDestination";

    [SubscribeLocalEvent]
    private void Startup(Entity<LinkedEntityContainerComponent> ent, ref MapInitEvent args)
    {
        var newEnt = PredictedSpawnNextToOrDrop(_portalDestination, ent.Owner);
        ent.Comp.Uid = newEnt;
    }

    public void SpawnWithLinkedEntity(string? protoName, Entity<TransformComponent> parent)
    {
        var uid = PredictedSpawnAtPosition(protoName, parent.Comp.Coordinates);
        var Comp = EnsureComp<LinkedEntityContainerComponent>(uid);
    }
}
