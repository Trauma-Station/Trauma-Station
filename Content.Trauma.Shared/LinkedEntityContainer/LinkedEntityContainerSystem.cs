// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Teleportation.Components;

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Spawns an ent on startup and stores its uid.
/// </summary>
public sealed partial class LinkedEntityContainerSystem : EntitySystem
{
    private EntProtoId<PortalComponent> _portalDestination = "ThronglerToy"; //"BluespaceRiftPortalDestination";

    [SubscribeLocalEvent]
    private void Startup(Entity<LinkedEntityContainerComponent> ent, ref ComponentStartup args)
    {
        var xform = Transform(ent);

        var newEnt = PredictedSpawnNextToOrDrop(_portalDestination, ent.Owner);
        // todo ensure Transform... i'll never escape this shitcode

        ent.Comp.NetEnt = GetNetEntity(newEnt);
        Log.Info($"{newEnt}");
        Dirty(ent.Owner, ent.Comp);
    }

    public void SpawnWithLinkedEntity(string? protoName, Entity<TransformComponent> parent)
    {
        var uid = PredictedSpawnAtPosition(protoName, parent.Comp.Coordinates);
        var Comp = EnsureComp<LinkedEntityContainerComponent>(uid);
    }
}
