// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Trauma.Shared.Containers;
using Content.Trauma.Shared.Spawners;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Spawners;

public sealed class DropPodSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly DropPodSystem _dropPod = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private List<EntityUid> _points = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: [ typeof(SpawnPointSystem) ]);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        // leave latejoins alone
        if (args.SpawnResult != null || _ticker.RunLevel == GameRunLevel.InRound)
            return;

        _points.Clear();
        var query = EntityQueryEnumerator<DropPodSpawnPointComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (args.Station is {} station && _station.GetOwningStation(uid) != station)
                continue;

            _points.Add(uid);
        }

        if (_points.Count == 0)
            return; // no drop pod spawnpoints, let regular spawn points handle it

        var point = _random.Pick(_points);
        var coords = Transform(point).Coordinates;

        // spawn the player as usual...
        var mob = _spawning.SpawnPlayerMob(coords,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
        args.SpawnResult = mob;

        // now put the mob inside of a drop pod!
        _dropPod.MakeDropPod(mob);
    }
}
