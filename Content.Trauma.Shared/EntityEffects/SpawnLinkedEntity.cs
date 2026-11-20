// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns an ent that will be destination target for linked portal.
/// </summary>
public sealed partial class SpawnLinkedEntity : EntityEffectBase<SpawnLinkedEntity>
{
    /// <summary>
    /// Destination target ent for linked portal.
    /// </summary>
    [DataField]
    public EntProtoId LinkedEnt = "BluespaceRiftPortalDestination";
}

public sealed partial class SpawnLinkedEntityEffectSystem : EntityEffectSystem<LinkedEntityComponent, SpawnLinkedEntity>
{
    [Dependency] private LinkedEntitySystem _link = default!;

    protected override void Effect(Entity<LinkedEntityComponent> ent, ref EntityEffectEvent<SpawnLinkedEntity> args)
    {
        var coords = Transform(ent).Coordinates;
        var newEnt = PredictedSpawnAtPosition(args.Effect.LinkedEnt, coords);
        _link.OneWayLink(ent, newEnt);
    }
}
