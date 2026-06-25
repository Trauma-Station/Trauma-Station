using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent>();
    }

    /// <summary>
    /// Queries all gamerules, regardless of if they're active or not.
    /// </summary>
    protected EntityQueryEnumerator<T, GameRuleComponent> QueryAllRules()
    {
        return EntityQueryEnumerator<T, GameRuleComponent>();
    }

    /// <summary>
    ///     Utility function for finding a random event-eligible station entity
    /// </summary>
    protected bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[RobustRandom.Next(stations.Count)];
        return true;
    }

    protected bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    // <Trauma> - refactored this method. Split into 3 smaller methods and made it so that it picks main station grid.
    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

        if (GetStationMainGrid(station) is not { } grid)
            return false;

        targetGrid = grid.Owner;
        return TryFindTileOnGrid(grid, out tile, out targetCoords);
    }
    // </Trauma>

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }
}
