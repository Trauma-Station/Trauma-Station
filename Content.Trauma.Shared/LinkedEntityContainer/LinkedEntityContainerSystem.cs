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
        if (ent.Comp.NetEnt != default &&
            ent.Comp.NetEnt != NetEntity.Invalid &&
            ent.Comp.NetEnt != NetEntity.First)
            return;

        var newEnt = PredictedSpawnNextToOrDrop(_portalDestination, ent.Owner);
        ent.Comp.NetEnt = GetNetEntity(newEnt);
        Dirty(ent.Owner, ent.Comp);
    }

    public void SpawnWithLinkedEntity(string? protoName, Entity<TransformComponent> parent)
    {
        var uid = PredictedSpawnAtPosition(protoName, parent.Comp.Coordinates);
        var Comp = EnsureComp<LinkedEntityContainerComponent>(uid);

        var newEnt = PredictedSpawnAtPosition(_portalDestination, parent.Comp.Coordinates);
        Comp.NetEnt = GetNetEntity(newEnt);
        Dirty(uid, Comp);
    }
}
