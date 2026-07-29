// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Blob;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Nutrition.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Blob.StationEvents;

public sealed partial class BlobSpawnRule : StationEventSystem<BlobSpawnRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPlayerManager _playerSystem = default!;

    public static readonly EntProtoId BlobRule = "BlobRule";

    protected override void Started(EntityUid uid,
        BlobSpawnRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (GetRandomStationGrids() is not { } stationGrids)
            return;

        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        while (locations.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid is { } grid && stationGrids.Contains(grid))
                validLocations.Add(xform.Coordinates);
        }

        if (validLocations.Count == 0)
        {
            Sawmill.Warning("There was no valid spawn points for blob!");
            return;
        }

        var playerPool = _playerSystem.Sessions.ToList();
        var numBlobs = MathHelper.Clamp(playerPool.Count / component.PlayersPerCarrierBlob, 1, component.MaxCarrierBlob);

        for (var i = 0; i < numBlobs; i++)
        {
            var coords = _random.Pick(validLocations);
            Sawmill.Info($"Creating carrier blob at {coords}");
            Spawn(_random.Pick(component.CarrierBlobProtos), coords);
        }

        // start blob rule incase it isn't, for the sweet greentext
        GameTicker.StartGameRule(BlobRule);
    }

    // Because GameRule spawns just a GhostRoleSpawner, we can't just remove components
    // right away, and need to track the event when entity is spawned.
    [SubscribeLocalEvent]
    private void OnSpawned(Entity<BlobCarrierComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        var carrier = args.Spawned;
        if (!HasComp<BlobCarrierComponent>(carrier))
            return;

        // Blob doesn't spawn when blob carrier was eaten.
        RemComp<EdibleComponent>(carrier);
    }
}
