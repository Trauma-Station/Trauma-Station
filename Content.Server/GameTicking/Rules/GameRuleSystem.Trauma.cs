using System.Diagnostics.CodeAnalysis;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Maps;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    [Dependency] private StationSystem _station = default!;
    [Dependency] private TurfSystem _turf = default!;

    /// <summary>
    /// Get a random station's OwnedGrids.
    /// Almost every gamerule should be using station's owned grids instead of station members so they dont hit cargo shuttle and stuff.
    /// </summary>
    public HashSet<EntityUid>? GetRandomStationGrids(out EntityUid? station)
        => TryGetRandomStation(out station) && TryComp<StationDataComponent>(station, out var data)
            ? data.OwnedGrids
            : null;

    public HashSet<EntityUid>? GetRandomStationGrids()
        => GetRandomStationGrids(out _);

    protected Entity<MapGridComponent>? GetStationMainGrid(Entity<StationDataComponent> station)
    {
        if (GetStationGridUid(station) is not {} grid ||
            !TryComp(grid, out MapGridComponent? gridComp))
            return null;

        return (grid, gridComp);
    }

    protected EntityUid? GetStationGridUid(Entity<StationDataComponent> station)
    {
        // first owned grid
        foreach (var grid in station.Comp.OwnedGrids)
        {
            return grid;
        }

        // use members if there are somehow no owned grids
        return _station.GetLargestGrid((station, station));
    }

    protected bool TryFindTileOnGrid(Entity<MapGridComponent> grid,
        out Vector2i tile,
        out EntityCoordinates targetCoords,
        int tries = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;

        var aabb = grid.Comp.LocalAABB;

        for (var i = 0; i < tries; i++)
        {
            var randomX = RobustRandom.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = RobustRandom.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);

            if (!_map.TryGetTile(grid.Comp, tile, out var selectedTile) || selectedTile.IsEmpty ||
                _turf.IsSpace(selectedTile))
                continue;

            if (_atmosphere.IsTileSpace(grid.Owner, Transform(grid.Owner).MapUid, tile)
                || _atmosphere.IsTileAirBlockedCached(grid.Owner, tile))
                continue;

            targetCoords = _map.GridTileToLocal(grid.Owner, grid.Comp, tile);
            return true;
        }

        return false;
    }
    // Goobstation end
}
