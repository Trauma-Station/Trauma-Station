// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Goobstation.Server.Antag.MaintsSpawn;

public sealed class MaintsSpawnRule : StationEventSystem<MaintsSpawnRuleComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityQuery<StationMemberComponent> _memberQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaintsSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, MaintsSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        // TODO: this is evil, make a random area thing that works something like this
        // 0. build an AABB of every area we want
        // 1. pick random position
        // 2. repeat until position is in wanted area
        var locations = EntityQueryEnumerator<MaintsSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<MapCoordinates>();
        while (locations.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid is not {} grid || _memberQuery.CompOrNull(grid)?.Station != station)
                continue;

            var coords = xform.Coordinates; // areas should always be parented to a grid, just round the coords
            var tile = new Vector2i((int) coords.X, (int) coords.Y);
            if (_atmos.IsTileAirBlockedCached(grid, tile))
                continue;

            validLocations.Add(_transform.GetMapCoordinates(xform));
        }

        if (validLocations.Count == 0)
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        component.Coords = validLocations;
    }

    private void OnSelectLocation(Entity<MaintsSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is {} coords)
            args.Coordinates.AddRange(coords);
    }
}
