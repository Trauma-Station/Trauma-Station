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
        if (ent.Comp.Spawned) // for some fucking reason it spawns twice even with the flag
            return;
        Log.Info($"startup: {ent.Comp.Spawned}");
        var newEnt = PredictedSpawnNextToOrDrop(_portalDestination, ent.Owner);
        ent.Comp.NetEnt = GetNetEntity(newEnt);
        Log.Info($"startup: new ent spawned {ent.Comp.NetEnt.Id}");
        ent.Comp.Spawned = true;
        Dirty(ent.Owner, ent.Comp);
        Log.Info($"startup: {ent.Comp.Spawned}");
    }

    public void SpawnWithLinkedEntity(string? protoName, Entity<TransformComponent> parent)
    {
        var uid = PredictedSpawnAtPosition(protoName, parent.Comp.Coordinates);
        var Comp = EnsureComp<LinkedEntityContainerComponent>(uid);

        var newEnt = PredictedSpawnAtPosition(_portalDestination, parent.Comp.Coordinates);
        Comp.NetEnt = GetNetEntity(newEnt);
        Log.Info($"spawn: new ent spawned {ent.Comp.NetEnt.Id}");
        Comp.Spawned = true;
        Dirty(uid, Comp);
        Log.Info($"spawn: {Comp.Spawned}");
    }
}
