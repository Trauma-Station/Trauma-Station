// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Teleportation.Components;

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Spawns an ent on startup and stores its uid.
/// </summary>
public sealed partial class LinkedEntityContainerSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    private EntProtoId<PortalComponent> _portalDestination = "ThronglerToy"; //"BluespaceRiftPortalDestination";

    [SubscribeLocalEvent]
    private void Startup(Entity<LinkedEntityContainerComponent> ent, ref ComponentStartup args)
    {
        var newEnt = PredictedSpawnNextToOrDrop(_portalDestination, ent.Owner);

        // var newXform = Transform(newEnt);
        // if (newXform.Coordinates.Position == Vector2.Zero || newXform.Coordinates.Position == Vector2Helpers.NaN)
        //     _transform.SetCoordinates(newEnt, Transform(ent).Coordinates);
        // Dirty(newEnt, newXform);

        Log.Info($"newEnt {newEnt}");

        ent.Comp.NetEnt = GetNetEntity(newEnt);
        Dirty(ent.Owner, ent.Comp);
        Log.Info($"NetEnt {ent.Comp.NetEnt}");
    }

    public void SpawnWithLinkedEntity(string? protoName, Entity<TransformComponent> parent)
    {
        var uid = PredictedSpawnAtPosition(protoName, parent.Comp.Coordinates);
        var Comp = EnsureComp<LinkedEntityContainerComponent>(uid);
    }
}
