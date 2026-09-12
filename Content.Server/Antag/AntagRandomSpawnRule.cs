// <Trauma>
using Content.Server.Station.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Map.Components;
// </Trauma>
using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Antag;

public sealed partial class AntagRandomSpawnSystem : GameRuleSystem<AntagRandomSpawnComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, AntagRandomSpawnComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        // we have to select this here because AntagSelectLocationEvent is raised twice because MakeAntag is called twice
        // once when a ghost role spawner is created and once when someone takes the ghost role

        if (TryFindRandomTile(out _, out _, out _, out var coords))
            comp.Coords = coords;
    }

    private void OnSelectLocation(Entity<AntagRandomSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords != null)
            args.Coordinates.Add(_transform.ToMapCoordinates(ent.Comp.Coords.Value));
        // <Trauma> if nothing was pre-selected, try again now to avoid nullspace.
        else if (TryFindRandomTile(out _, out _, out _, out var coords))
        {
            args.Coordinates.Add(_transform.ToMapCoordinates(coords));
        }
        else
        {
            // fuck this chud heisentest
            var stations = new List<string>();
            var grids = new List<string>();
            foreach (var station in AllEntityQuery<StationEventEligibleComponent, StationDataComponent>())
            {
                var mainGrid = GetStationMainGrid((station, station.Comp2));
                stations.Add($"- {ToPrettyString(station)}: Main grid {ToPrettyString(mainGrid)}");
                foreach (var grid in station.Comp2.OwnedGrids)
                {
                    var gridComp = Comp<MapGridComponent>(grid);
                    var count = Map.GetFilledTileCount((grid, gridComp));
                    grids.Add($"- {ToPrettyString(grid)} @ {Transform(grid).Coordinates} with {count} filled tiles");
                }
            }

            var stationNames = string.Join("\n", stations);
            var gridNames = string.Join("\n", grids);
            Log.Error($"Failed to find a random tile for rule {ToPrettyString(ent)} spawning {args.Antag.ID} for player {args.Session}.\nStations:\n{stationNames}\nStation grids:\n{gridNames}");
        }
        // </Trauma>
    }
}
