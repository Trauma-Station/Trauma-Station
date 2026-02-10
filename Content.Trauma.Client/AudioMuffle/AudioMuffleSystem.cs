// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Movement.Components;
using Content.Shared.StationAi;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.AudioMuffle;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Trauma.Client.AudioMuffle;

public sealed partial class AudioMuffleSystem : SharedAudioMuffleSystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static EntityQuery<GhostComponent> _ghostQuery;
    private static EntityQuery<SpectralComponent> _spectralQuery;
    private static EntityQuery<RelayInputMoverComponent> _relayedQuery;
    private static EntityQuery<AiEyeComponent> _aiEyeQuery;
    private static EntityQuery<SoundBlockerComponent> _blockerQuery;

    // Tile indices -> blocker entities
    [ViewVariables]
    public readonly Dictionary<Vector2i, HashSet<Entity<SoundBlockerComponent>>> ReverseBlockerIndicesDict = new();

    // Tile indices -> data
    [ViewVariables]
    public readonly Dictionary<Vector2i, MuffleTileData> TileDataDict = new();

    [ViewVariables]
    public Entity<MapGridComponent>? PlayerGrid;

    [ViewVariables]
    public Vector2i? OldPlayerTile;

    private const int AudioRange = (int) SharedAudioSystem.DefaultSoundRange;

    // sqrt(2 * AudioRange^2)
    private const int PathfindingRange = 22;

    private bool _pathfindingEnabled = true;
    private float _maxRayLength;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _spectralQuery = GetEntityQuery<SpectralComponent>();
        _relayedQuery = GetEntityQuery<RelayInputMoverComponent>();
        _aiEyeQuery = GetEntityQuery<AiEyeComponent>();
        _blockerQuery = GetEntityQuery<SoundBlockerComponent>();

        _xform.OnGlobalMoveEvent += OnMove;

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);

        SubscribeLocalEvent<SoundBlockerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SoundBlockerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SoundBlockerComponent, AfterAutoHandleStateEvent>(OnBlockerState);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRestart);

        _audio.GetOcclusionOverride += OnOcclusion;

        Subs.CVar(_cfg, TraumaCVars.AudioMufflePathfinding, value => _pathfindingEnabled = value, true);
        Subs.CVar(_cfg, CVars.AudioRaycastLength, value => _maxRayLength = value, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        PlayerGrid = null;
        OldPlayerTile = null;
        ClearDicts();

        _xform.OnGlobalMoveEvent -= OnMove;

        _audio.GetOcclusionOverride -= OnOcclusion;
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        PlayerGrid = null;
        OldPlayerTile = null;
        ClearDicts();
    }

    private void ResetImmediate(EntityUid player)
    {
        ClearDicts();
        ResetAllBlockers(player);
    }

    private void ResetAllBlockers(EntityUid player)
    {
        if (!_pathfindingEnabled)
            return;

        var query = EntityQueryEnumerator<SoundBlockerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var blocker, out var xform))
        {
            ResetBlockerMuffle(player, (uid, xform, blocker));
        }
    }

    private void OnBlockerState(Entity<SoundBlockerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ent.Comp.CachedBlockerCost = null;

        if (ResolvePlayer() is not { } player)
            return;

        ResetBlockerMuffle(player, (ent, null, ent));
    }

    private void OnStartup(Entity<SoundBlockerComponent> ent, ref ComponentStartup args)
    {
        if (ResolvePlayer() is not { } player)
            return;

        ResetBlockerMuffle(player, (ent, null, ent));
    }

    private void OnShutdown(Entity<SoundBlockerComponent> ent, ref ComponentShutdown args)
    {
        RemoveBlocker(ent);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        TileDataDict.Clear();

        if (!_pathfindingEnabled)
            return;

        var pos = _xform.GetMapCoordinates(ev.Entity);
        if (ResolvePlayerGrid(pos) is not { } grid)
            return;

        var tile = _map.TileIndicesFor(grid, pos);

        if (!_map.CollidesWithGrid(grid, grid, tile))
            return;

        Expand(tile);
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        TileDataDict.Clear();
    }

    private void OnMove(ref MoveEvent ev)
    {
        if (!_pathfindingEnabled)
            return;

        if (ev.OldPosition == ev.NewPosition)
            return;

        if (ResolvePlayer() is not { } player)
            return;

        var uid = ev.Entity.Owner;

        if (HasComp<MapGridComponent>(uid))
            return;

        var oldMap = ev.OldPosition.IsValid(EntityManager)
            ? _xform.ToMapCoordinates(ev.OldPosition)
            : MapCoordinates.Nullspace;
        var newMap = ev.NewPosition.IsValid(EntityManager)
            ? _xform.ToMapCoordinates(ev.NewPosition)
            : MapCoordinates.Nullspace;

        if (oldMap == MapCoordinates.Nullspace && newMap == MapCoordinates.Nullspace)
            return;

        ProcessEntityMove(player, uid, oldMap, newMap);

        var childEnumerator = ev.Entity.Comp1.ChildEnumerator;
        while (childEnumerator.MoveNext(out var child))
        {
            ProcessEntityMove(player, child, oldMap, newMap);
        }
    }

    private void ProcessEntityMove(EntityUid player,
        EntityUid uid,
        MapCoordinates oldPosition,
        MapCoordinates newPosition)
    {
        // ResolvePlayer returns "fake" player (ai vision entity) if local entity is ai eye
        if (_relayedQuery.TryComp(_player.LocalEntity, out var relay) &&
            uid == relay.RelayEntity && player != relay.RelayEntity)
        {
            PlayerMoved(player, MapCoordinates.Nullspace, _xform.GetMapCoordinates(player));
            return;
        }

        if (uid == player)
        {
            PlayerMoved(player, oldPosition, newPosition);
            return;
        }

        if (_blockerQuery.TryComp(uid, out var blocker))
        {
            ResetBlockerMuffle(player, (uid, null, blocker), oldPosition, newPosition);
        }
    }

    private void ClearDicts()
    {
        TileDataDict.Clear();
        ReverseBlockerIndicesDict.Clear();
    }

    public EntityUid? ResolvePlayer()
    {
        if (_player.LocalEntity is not { } player)
            return null;

        if (_relayedQuery.TryComp(player, out var relay) && _aiEyeQuery.HasComp(relay.RelayEntity))
        {
            if (FindNearestAiVisionEntity(relay.RelayEntity) is { } entity)
                return entity;

            return relay.RelayEntity;
        }

        if (_ghostQuery.HasComp(player) || _spectralQuery.HasComp(player))
            return null;

        return player;
    }

    public EntityUid? FindNearestAiVisionEntity(EntityUid player)
    {
        var coords = _xform.GetMapCoordinates(player);
        var nearest = _lookup.GetEntitiesInRange<StationAiVisionComponent>(coords, AudioRange);
        EntityUid? result = null;
        var distance = float.MaxValue;
        foreach (var (uid, vision) in nearest)
        {
            if (!vision.Enabled)
                continue;

            var dist = (coords.Position - _xform.GetMapCoordinates(uid).Position).Length();

            if (result != null && dist >= distance)
                continue;

            result = uid;
            distance = dist;
        }

        return result;
    }

    public Entity<MapGridComponent>? ResolvePlayerGrid(MapCoordinates pos)
    {
        if (Exists(PlayerGrid) && !PlayerGrid.Value.Comp.Deleted &&
            _xform.GetMapId(PlayerGrid.Value.Owner) == pos.MapId)
            return PlayerGrid.Value;

        if (_mapManager.TryFindGridAt(pos, out var grid, out var gridComp))
            PlayerGrid = (grid, gridComp);
        else
            PlayerGrid = null;

        return PlayerGrid;
    }

    private void RemoveBlocker(Entity<SoundBlockerComponent> blocker)
    {
        if (blocker.Comp.Indices is { } blockerIndices)
            AddOrRemoveBlocker(blocker, blockerIndices, false, true);
    }

    private void PlayerMoved(EntityUid player, MapCoordinates oldPos, MapCoordinates newPos)
    {
        if (!_pathfindingEnabled)
            return;

        if (newPos == MapCoordinates.Nullspace)
            return;

        if (oldPos.MapId != newPos.MapId || !Exists(PlayerGrid))
        {
            PlayerGrid = null;
            OldPlayerTile = null;
            if (_mapManager.TryFindGridAt(newPos, out var g, out var gC))
            {
                PlayerGrid = (g, gC);
                var tile = _map.TileIndicesFor((g, gC), newPos);
                Expand(tile);
                return;
            }

            ResetImmediate(player);
            return;
        }

        if (!_mapManager.TryFindGridAt(newPos, out var grid, out var gridComp))
        {
            PlayerGrid = null;
            OldPlayerTile = null;
            return;
        }

        var tileNew = _map.TileIndicesFor((grid, gridComp), newPos);

        if (grid != PlayerGrid.Value.Owner)
        {
            PlayerGrid = (grid, gridComp);
            Expand(tileNew);
            return;
        }

        if (oldPos == MapCoordinates.Nullspace)
        {
            Expand(tileNew);
            return;
        }

        var tileOld = _map.TileIndicesFor((grid, gridComp), oldPos);

        if (tileOld == tileNew)
        {
            if (OldPlayerTile != null && OldPlayerTile != tileNew)
            {
                RebuildAndExpand(tileNew, OldPlayerTile.Value);
                OldPlayerTile = tileNew;
            }

            return;
        }

        OldPlayerTile = tileNew;
        RebuildAndExpand(tileNew, tileOld);
    }

    private void ResetBlockerMuffle(EntityUid player,
        Entity<TransformComponent?, SoundBlockerComponent?> blocker,
        MapCoordinates? oldPosition = null,
        MapCoordinates? newPosition = null)
    {
        if (!_pathfindingEnabled)
            return;

        if (!Resolve(blocker, ref blocker.Comp1, ref blocker.Comp2, false))
            return;

        Entity<SoundBlockerComponent> blockerEnt = (blocker.Owner, blocker.Comp2);

        var playerXform = Transform(player);
        var blockerXform = blocker.Comp1;

        var blockerPos = newPosition;
        if (blockerPos == null || blockerPos == MapCoordinates.Nullspace)
            blockerPos = oldPosition;
        if (blockerPos == null || blockerPos == MapCoordinates.Nullspace)
            blockerPos = _xform.GetMapCoordinates(blocker.Owner, blockerXform);
        if (blockerPos == MapCoordinates.Nullspace)
        {
            RemoveBlocker(blockerEnt);
            return;
        }

        var pos = _xform.GetMapCoordinates(player, playerXform);

        var oldIndices = blockerEnt.Comp.Indices;

        if (pos == MapCoordinates.Nullspace)
        {
            if (!Exists(PlayerGrid) || PlayerGrid.Value.Comp.Deleted ||
                _xform.GetMapId(PlayerGrid.Value.Owner) != blockerPos.Value.MapId)
                return;

            ResetBlockerOnGrid(PlayerGrid.Value, blockerEnt, blockerPos.Value, oldIndices);
            return;
        }

        if (pos.MapId != blockerPos.Value.MapId)
        {
            if (blockerEnt.Comp.Indices is { } indices)
                oldIndices = indices;

            if (oldIndices == null)
                return;

            AddOrRemoveBlocker(blockerEnt, oldIndices.Value, false, true);
            return;
        }

        if (TryFindCommonPlayerGrid(pos, blockerPos.Value) is { } grid)
            ResetBlockerOnGrid(grid, blockerEnt, blockerPos.Value, oldIndices);
        else if (oldIndices != null)
            AddOrRemoveBlocker(blockerEnt, oldIndices.Value, false, true);
    }

    private void ResetBlockerOnGrid(Entity<MapGridComponent> grid,
        Entity<SoundBlockerComponent> blocker,
        MapCoordinates blockerPos,
        Vector2i? oldIndices)
    {
        var indices = _map.TileIndicesFor(grid, blockerPos);
        if (oldIndices != null)
        {
            if (indices == oldIndices.Value)
            {
                if (TileDataDict.TryGetValue(indices, out var data))
                {
                    var curCost = data.TotalCost;
                    var baseCost = (data.Previous?.TotalCost ?? -1f) + 1f;
                    var totalCost = GetTotalTileCost(indices);
                    var sum = baseCost + totalCost;
                    var delta = sum - curCost;
                    if (MathHelper.CloseToPercent(delta, 0f))
                        return;

                    ModifyBlockerAmount(data, delta);
                }

                return;
            }

            AddOrRemoveBlocker(blocker, oldIndices.Value, false, true);
        }

        AddOrRemoveBlocker(blocker, indices, true, true);
    }

    public Entity<MapGridComponent>? TryFindCommonPlayerGrid(MapCoordinates pos, MapCoordinates other)
    {
        if (ResolvePlayerGrid(pos) is { } grid &&
            _mapManager.TryFindGridAt(other, out var gridB, out _) && grid.Owner == gridB)
            return grid;

        return null;
    }

    public float GetTotalTileCost(Vector2i tile)
    {
        if (!ReverseBlockerIndicesDict.TryGetValue(tile, out var blockers))
            return 0f;

        var total = 0f;
        var toRemove = new List<Entity<SoundBlockerComponent>>();
        foreach (var blocker in blockers)
        {
            if (!Exists(blocker))
            {
                toRemove.Add(blocker);
                continue;
            }

            total += GetBlockerCost(blocker.Comp);
        }

        foreach (var remove in toRemove)
        {
            remove.Comp.Indices = null;
            blockers.Remove(remove);
        }

        if (blockers.Count == 0)
            ReverseBlockerIndicesDict.Remove(tile);

        return total;
    }

    public static float GetBlockerCost(SoundBlockerComponent blocker)
    {
        if (!blocker.Active)
            return 0f;

        if (blocker.CachedBlockerCost == null)
        {
            var percent = MathF.Max(blocker.SoundBlockPercent, 0f);
            blocker.CachedBlockerCost = percent > 0.99f ? 400f : -(1f / (percent - 1f)) * 4f - 4f;
        }

        return blocker.CachedBlockerCost.Value;
    }

    private float OnOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt)
    {
        if (distance < 0.1f || ResolvePlayer() is not { } player)
            return 0f;

        // ResolvePlayer returns nearest entity that provides ai vision, if it cannot find any, it returns ai eye
        // itself, which means no cameras nearby => all audio is muffled
        if (distance > AudioRange || _aiEyeQuery.HasComp(player))
            return 100f;

        if (!_pathfindingEnabled)
            return CalculateRaycastOcclusion(listener, delta, distance, ignoredEnt);

        var xform = Transform(player);
        var playerPos = _xform.GetMapCoordinates(player, xform);
        var audioPos = new MapCoordinates(listener.Position + delta, listener.MapId);
        delta = audioPos.Position - playerPos.Position;
        distance = delta.Length();

        if (TryFindCommonPlayerGrid(playerPos, audioPos) is not { } grid)
            return CalculateRaycastOcclusion(listener, delta, distance, ignoredEnt);

        var tile = _map.TileIndicesFor(grid, audioPos);

        return !TileDataDict.TryGetValue(tile, out var data)
            ? CalculateRaycastOcclusion(listener, delta, distance, ignoredEnt)
            : CalculatePathfindingOcclusion(grid, playerPos, tile, data);
    }

    private float CalculatePathfindingOcclusion(Entity<MapGridComponent> grid,
        MapCoordinates playerPos,
        Vector2i pos,
        MuffleTileData tileData)
    {
        var playerIndices = _map.TileIndicesFor(grid, playerPos);
        var playerDist = (float) ManhattanDistance(pos, playerIndices);
        var muffleLevel = tileData.TotalCost + (playerDist - AudioRange) / 4f - GetTotalTileCost(pos);
        return CalculateOcclusion(muffleLevel);
    }

    private float CalculateRaycastOcclusion(MapCoordinates listener,
        Vector2 delta,
        float distance,
        EntityUid? ignoredEnt)
    {
        var rayLength = MathF.Min(distance, _maxRayLength);
        var ray = new CollisionRay(listener.Position, delta / distance, _audio.OcclusionCollisionMask);

        var results = _physics.IntersectRayWithPredicate(listener.MapId,
            ray,
            rayLength,
            x => x == ignoredEnt || !_blockerQuery.HasComp(x),
            false);

        var muffleLevel = 0f;
        foreach (var result in results)
        {
            muffleLevel += GetBlockerCost(_blockerQuery.Comp(result.HitEntity));
        }

        return CalculateOcclusion(muffleLevel + distance);
    }

    private static float CalculateOcclusion(float muffleLevel)
    {
        return MathF.Pow(muffleLevel / 8f, 4f);
    }
}
