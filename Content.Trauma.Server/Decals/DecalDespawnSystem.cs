// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Decals;
using Content.Shared.GameTicking;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Decals;
using Content.Trauma.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Decals;

/// <summary>
/// Manages decals that opt in to being removed after a delay, configured by cvars.
/// </summary>
public sealed class DecalDespawnSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimedRingBuffer<(EntityUid, uint)> _buffer = default!;

    private int _limit;
    private TimeSpan _despawnTime;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DespawningDecalSpawnerComponent, DecalSpawnedEvent>(OnDecalSpawned);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        Subs.CVar(_cfg, TraumaCVars.DecalDespawnLimit, x => _limit = x, true);
        Subs.CVar(_cfg, TraumaCVars.DecalDespawnTime, UpdateDespawnTime, true);

        _buffer = new(_limit, _despawnTime, _timing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // only removes 1 per tick max because of the timed buffer, basically 0 cost
        if (_buffer.PopNext(out var next) && Exists(next.Item1))
            _decal.RemoveDecal(next.Item1, next.Item2);
    }

    private void OnDecalSpawned(Entity<DespawningDecalSpawnerComponent> ent, ref DecalSpawnedEvent args)
    {
        QueueDespawn(args.Grid, args.Decal);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        // if someone changed the cvar it can be used now since old values wont matter
        _buffer.Reset(_limit);
    }

    private void UpdateDespawnTime(float seconds)
    {
        _despawnTime = TimeSpan.FromSeconds(seconds);
        if (_buffer == default)
            return; // it will be used when creating it

        _buffer.PopDelay = _despawnTime;
    }

    /// <summary>
    /// Queue the despawning of a given decal on a grid.
    /// If there are too many decals despawning at once, the oldest one will be immediately removed.
    /// </summary>
    public void QueueDespawn(EntityUid grid, uint decal)
    {
        DebugTools.Assert(HasComp<MapGridComponent>(grid), $"{ToPrettyString(grid)} is not a grid!");
        if (_buffer.Push((grid, decal), out var old) && Exists(old.Item1))
            _decal.RemoveDecal(old.Item1, old.Item2);
    }
}
