using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Station.Systems;

public sealed partial class ServerStationSystem
{
    [Dependency] private TurfSystem _turf = default!;

    public override bool TryFindTileOnGrid(Entity<MapGridComponent> grid,
        out Vector2i tile,
        out EntityCoordinates targetCoords,
        int tries = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;

        var aabb = grid.Comp.LocalAABB;

        for (var i = 0; i < tries; i++)
        {
            var randomX = Random.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = Random.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);

            if (!Map.TryGetTile(grid.Comp, tile, out var selectedTile) || selectedTile.IsEmpty ||
                _turf.IsSpace(selectedTile))
                continue;

            if (_atmos.IsTileSpace(grid.Owner, Transform(grid.Owner).MapUid, tile)
                || _atmos.IsTileAirBlockedCached(grid.Owner, tile))
                continue;

            targetCoords = Map.GridTileToLocal(grid.Owner, grid.Comp, tile);
            return true;
        }

        return false;
    }
}
