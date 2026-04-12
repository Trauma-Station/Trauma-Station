using Content.Server.Antag;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Station event component for spawning this rules antags in space around a station.
/// </summary>
public sealed class SpaceSpawnRule : StationEventSystem<SpaceSpawnRuleComponent>
{
    // <Trauma>
    [Dependency] private readonly IMapManager _map = default!;
    // </Trauma>
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, SpaceSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        // find a station grid
        var gridUid = StationSystem.GetLargestGrid(station.Value);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Sawmill.Warning("Chosen station has no grids, cannot pick location for {ToPrettyString(uid):rule}");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // figure out its AABB size and use that as a guide to how far the spawner should be
        var size = grid.LocalAABB.Size.Length() / 2;
        var distance = size + comp.SpawnDistance;
        // <Trauma> - tries20 this shit and check that it's actually in space
        var xform = Transform(gridUid.Value);
        var center = _transform.GetWorldPosition(xform); // don't need to recheck this for every try
        for (int i = 0; i < 20; i++)
        {
            var angle = RobustRandom.NextAngle();
            // position relative to station center
            var location = angle.ToVec() * distance;

            var position = center + location;
            if (_map.TryFindGridAt(xform.MapUid!.Value, position, out _, out _))
                continue; // it's not in space it's on a grid, pick again

            // create the spawner!
            comp.Coords = new MapCoordinates(position, xform.MapID);
            Sawmill.Info($"Picked location {comp.Coords} for {ToPrettyString(uid):rule}");
            break;
        }
        // <Trauma>
    }

    private void OnSelectLocation(Entity<SpaceSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is {} coords)
            args.Coordinates.Add(coords);
    }
}
