// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Spawns a random number of entities at the target.
/// <see cref="Number"/> is the inclusive maximum number of entities to spawn, the minimum is <see cref="Min"/>.
/// </summary>
public sealed partial class SpawnTile : EntityEffectBase<SpawnTile>
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;
}

public sealed partial class SpawnTileEffectSystem : EntityEffectSystem<TransformComponent, SpawnTile>
{
    [Dependency] private ITileDefinitionManager _tileMan = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TileSystem _tile = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnTile> args)
    {
        var xform = Transform(ent.Owner);
        var mapCoords = _transform.GetMapCoordinates(ent.Owner, xform);
        if (!_map.TryFindGridAt(mapCoords, out var gridUid, out var gridComp) ||
            !_map.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsEmpty)
        {
            return;
        }

        var newTile = (ContentTileDefinition) _tileMan[args.Effect.Tile];

        _tile.ReplaceTile(tileRef, newTile);
    }
}
