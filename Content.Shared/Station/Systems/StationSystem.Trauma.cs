using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Station.Systems;

public abstract partial class StationSystem
{
    /// <summary>
    /// Get a random station's OwnedGrids.
    /// Almost every gamerule should be using station's owned grids instead of station members so they dont hit cargo shuttle and stuff.
    /// </summary>
    public HashSet<EntityUid>? GetRandomStationGrids(out Entity<StationDataComponent>? station)
        => TryGetRandomStation(out station)
            ? station.Value.Comp.OwnedGrids
            : null;

    public HashSet<EntityUid>? GetRandomStationGrids()
        => GetRandomStationGrids(out _);

    public Entity<MapGridComponent>? GetStationMainGrid(Entity<StationDataComponent> station)
    {
        if (GetStationGridUid(station) is not {} grid ||
            !TryComp(grid, out MapGridComponent? gridComp))
            return null;

        return (grid, gridComp);
    }

    public EntityUid? GetStationGridUid(Entity<StationDataComponent> station)
    {
        // first owned grid
        foreach (var grid in station.Comp.OwnedGrids)
        {
            return grid;
        }

        // use members if there are somehow no owned grids
        return GetLargestGrid((station, station));
    }

    public virtual bool TryFindTileOnGrid(Entity<MapGridComponent> grid,
        out Vector2i tile,
        out EntityCoordinates targetCoords,
        int tries = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        return false;
    }
}
