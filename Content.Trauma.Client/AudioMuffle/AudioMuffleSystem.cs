using System.Numerics;
using Content.Shared.Ghost;
using Content.Shared.Physics;
using Content.Trauma.Shared.AudioMuffle;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.AudioMuffle;

public sealed partial class AudioMuffleSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MapSystem _map = default!;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClientGameStateManager _stateMan = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static EntityQuery<GhostComponent> _ghostQuery;
    private static EntityQuery<SpectralComponent> _spectralQuery;
    private static EntityQuery<AudioComponent> _audioQuery;
    private static EntityQuery<SoundBlockerComponent> _blockerQuery;

    // Blocker entity -> List<(ray direction between player and audio, Audio entity)>
    [ViewVariables]
    public readonly Dictionary<Entity<SoundBlockerComponent>, List<(Vector2, Entity<AudioComponent>)>>
        SoundBlockerDict = new();

    // Blocker entity -> tile indices
    [ViewVariables]
    public readonly Dictionary<Entity<SoundBlockerComponent>, Vector2i> BlockerIndicesDict = new();

    // Tile indices -> blocker entities
    [ViewVariables]
    public readonly Dictionary<Vector2i, HashSet<Entity<SoundBlockerComponent>>> ReverseBlockerIndicesDict = new();

    // Audio entity -> List of blocker entities
    [ViewVariables]
    public readonly Dictionary<Entity<AudioComponent>, HashSet<Entity<SoundBlockerComponent>>> ReverseSoundBlockerDict =
        new();

    // Audio entity -> volume
    [ViewVariables]
    public readonly Dictionary<Entity<AudioComponent>, float> AudioVolumeDict = new();

    // Audio entity -> tile indices
    [ViewVariables]
    public readonly Dictionary<Entity<AudioComponent>, Vector2i> AudioPosDict = new();

    // Tile indices -> list of audio entities
    [ViewVariables]
    public readonly Dictionary<Vector2i, HashSet<Entity<AudioComponent>>> ReverseAudioPosDict = new();

    // Tile indices -> data
    [ViewVariables]
    public readonly Dictionary<Vector2i, MuffleTileData> TileDataDict = new();

    [ViewVariables]
    public Entity<MapGridComponent>? PlayerGrid;

    [ViewVariables]
    public Vector2i? OldPlayerTile;

    private readonly List<Entity<AudioComponent>> _audioToRemove = new();

    private readonly List<Entity<SoundBlockerComponent>> _blockersToRemove = new();

    private const int AudioRange = (int) SharedAudioSystem.DefaultSoundRange;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _spectralQuery = GetEntityQuery<SpectralComponent>();
        _audioQuery = GetEntityQuery<AudioComponent>();
        _blockerQuery = GetEntityQuery<SoundBlockerComponent>();

        _xform.OnGlobalMoveEvent += OnMove;
        _stateMan.GameStateApplied += OnGameStateApplied;
        EntityManager.EntityDeleted += OnEntDeleted;
        EntityManager.EntityInitialized += OnEntInitialized;

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);

        SubscribeLocalEvent<SoundBlockerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SoundBlockerComponent, ComponentShutdown>(OnShutdown);
    }

    // TODO: reset on round restart
    public override void Shutdown()
    {
        base.Shutdown();

        PlayerGrid = null;
        OldPlayerTile = null;
        ClearDicts(true);

        _xform.OnGlobalMoveEvent -= OnMove;
        _stateMan.GameStateApplied -= OnGameStateApplied;
        EntityManager.EntityDeleted -= OnEntDeleted;
        EntityManager.EntityInitialized -= OnEntInitialized;

        _overlay.RemoveOverlay<AudioMuffleOverlay>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ResetAllPosAudio();
        ResetAllRaycastAudio();
    }

    private void ResetImmediate(EntityUid player, bool fullReset)
    {
        ClearDicts(fullReset);

        if (!fullReset)
            return;

        ReCalculateAllAudio(player);
        ResetAllBlockers(player);
    }

    private void ReCalculateAllAudio(EntityUid player)
    {
        var query = EntityQueryEnumerator<AudioComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var audio, out var xform))
        {
            ReCalculateAudioMuffle(player, (uid, audio), _xform.GetMapCoordinates(uid, xform), null, false);
        }
    }

    private void ResetAllBlockers(EntityUid player)
    {
        var query = EntityQueryEnumerator<SoundBlockerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var blocker, out var xform))
        {
            ResetBlockerMuffle(player, (uid, xform, blocker));
        }
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
        ResetImmediate(ev.Entity, true);

        var pos = _xform.GetMapCoordinates(ev.Entity);
        if (ResolvePlayerGrid(pos) is { } grid)
        {
            var tile = _map.TileIndicesFor(grid, pos);
            Expand(tile);
        }
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        ClearDicts(false);
        TileDataDict.Clear();
    }

    private void OnMove(ref MoveEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
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

        if (ProcessEntityMove(player, uid, oldMap, newMap))
            return;

        var childEnumerator = ev.Entity.Comp1.ChildEnumerator;
        while (childEnumerator.MoveNext(out var child))
        {
            if (ProcessEntityMove(player, child, oldMap, newMap))
                return;
        }
    }

    private bool ProcessEntityMove(EntityUid player,
        EntityUid uid,
        MapCoordinates oldPosition,
        MapCoordinates newPosition)
    {
        if (uid == player)
        {
            PlayerMoved(player, oldPosition, newPosition);
            return true;
        }

        if (_blockerQuery.TryComp(uid, out var blocker))
            ResetBlockerMuffle(player, (uid, null, blocker), oldPosition, newPosition);

        if (_audioQuery.TryComp(uid, out var audio))
            ReCalculateAudioMuffle(player, (uid, audio), newPosition);

        return false;
    }

    private void OnEntInitialized(Entity<MetaDataComponent> obj)
    {
        if (ResolvePlayer() is not { } player)
            return;

        ReCalculateAudioMuffle(player, obj.Owner, _xform.GetMapCoordinates(obj.Owner));
    }

    private void OnEntDeleted(Entity<MetaDataComponent> obj)
    {
        RemoveAudioMuffle(obj.Owner);
    }

    private void OnGameStateApplied(GameStateAppliedArgs args)
    {
        if (ResolvePlayer() is not { } player)
            return;

        // TODO: detached + pvs leave action
        foreach (var deleted in args.AppliedState.EntityDeletions.Value)
        {
            if (!TryGetEntity(deleted, out var ent))
                continue;

            RemoveAudioMuffle(ent.Value);
        }

        foreach (var states in args.AppliedState.EntityStates.Value)
        {
            if (!TryGetEntity(states.NetEntity, out var ent))
                continue;

            if (!_audioQuery.TryComp(ent.Value, out var audioComp))
                continue;

            float? volume = null;

            foreach (var change in states.ComponentChanges.Value)
            {
                if (change.State is not AudioComponent.AudioComponent_AutoState state)
                    continue;

                volume = state.Params.Volume;
                break;
            }

            ReCalculateAudioMuffle(player, (ent.Value, audioComp), _xform.GetMapCoordinates(ent.Value), volume);
        }
    }

    private void ClearDicts(bool clearAll)
    {
        SoundBlockerDict.Clear();
        ReverseSoundBlockerDict.Clear();

        if (!clearAll)
            return;

        AudioVolumeDict.Clear();
        TileDataDict.Clear();
        AudioPosDict.Clear();
        ReverseAudioPosDict.Clear();
        BlockerIndicesDict.Clear();
        ReverseBlockerIndicesDict.Clear();
    }

    public EntityUid? ResolvePlayer()
    {
        if (_player.LocalEntity is not { } player)
            return null;

        if (_ghostQuery.HasComp(player) || _spectralQuery.HasComp(player))
            return null;

        return player;
    }

    public Entity<MapGridComponent>? ResolvePlayerGrid(MapCoordinates pos)
    {
        if (Exists(PlayerGrid) && !PlayerGrid.Value.Comp.Deleted)
            return PlayerGrid.Value;

        if (_mapManager.TryFindGridAt(pos, out var grid, out var gridComp))
            PlayerGrid = (grid, gridComp);
        else
            PlayerGrid = null;

        return PlayerGrid;
    }

    private void RemoveBlocker(Entity<SoundBlockerComponent> blocker)
    {
        if (BlockerIndicesDict.Remove(blocker, out var blockerIndices))
            AddOrRemoveBlocker(blocker.AsNullable(), blockerIndices, false, true);

        if (!SoundBlockerDict.Remove(blocker, out var data))
            return;

        foreach (var (_, audio) in data)
        {
            if (!ReverseSoundBlockerDict.TryGetValue(audio, out var blockers))
                continue;

            blockers.Remove(blocker);

            if (blockers.Count == 0)
                ReverseSoundBlockerDict.Remove(audio);
        }
    }

    private void PlayerMoved(EntityUid player, MapCoordinates oldPos, MapCoordinates newPos)
    {
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
                ReCalculateAllAudio(player);
                ResetAllBlockers(player);
                return;
            }
            ResetImmediate(player, true);
            return;
        }

        if (_mapManager.TryFindGridAt(newPos, out var grid, out var gridComp))
        {
            var tileNew = _map.TileIndicesFor((grid, gridComp), newPos);

            if (grid != PlayerGrid.Value.Owner)
            {
                PlayerGrid = (grid, gridComp);
                Expand(tileNew);
                ReCalculateAllAudio(player);
                ResetAllBlockers(player);
                return;
            }

            if (oldPos == MapCoordinates.Nullspace)
            {
                Expand(tileNew);
                ReCalculateAllAudio(player);
                ResetAllBlockers(player);
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

                ResetAllPosAudio();
                ResetAllRaycastAudio();
                return;
            }

            OldPlayerTile = tileNew;
            RebuildAndExpand(tileNew, tileOld);
        }
        else
        {
            PlayerGrid = null;
            OldPlayerTile = null;
        }

        ResetAllPosAudio();
        ResetAllRaycastAudio();
    }

    private void ResetBlockerMuffle(EntityUid player,
        Entity<TransformComponent?, SoundBlockerComponent?> blocker,
        MapCoordinates? oldPosition = null,
        MapCoordinates? newPosition = null)
    {
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

        var found = BlockerIndicesDict.TryGetValue(blockerEnt, out var oldIndices);

        if (pos == MapCoordinates.Nullspace)
        {
            if (!Exists(PlayerGrid) || PlayerGrid.Value.Comp.Deleted)
                return;

            ResetBlockerOnGrid(PlayerGrid.Value, blocker, blockerPos.Value, found ? oldIndices : null);
            return;
        }

        if (pos.MapId != blockerPos.Value.MapId)
        {
            SoundBlockerDict.Remove(blockerEnt);
            if (BlockerIndicesDict.Remove(blockerEnt, out var indices))
            {
                found = true;
                oldIndices = indices;
            }

            if (!found)
                return;

            AddOrRemoveBlocker(blockerEnt.AsNullable(), oldIndices, false, true);
            return;
        }

        if (TryFindCommonPlayerGrid(pos, blockerPos.Value) is { } grid)
            ResetBlockerOnGrid(grid, blocker, blockerPos.Value, found ? oldIndices : null);
        else
        {
            BlockerIndicesDict.Remove(blockerEnt);
            AddOrRemoveBlocker(blockerEnt.AsNullable(), oldIndices, false, true);
        }

        if (!SoundBlockerDict.TryGetValue(blockerEnt, out var data))
            data = new List<(Vector2, Entity<AudioComponent>)>();

        var aabb = CalculateAABB((blocker, null, blockerXform));
        if (aabb == null)
            return;

        if (aabb.Value.Box.IsEmpty())
        {
            SoundBlockerDict.Remove(blockerEnt);
            return;
        }

        var minAngleTheta = 0f;
        var maxAngleTheta = 0f;

        if (aabb.Value.Contains(pos.Position))
        {
            GatherAudio(blockerEnt, pos.Position, data, null);
            SoundBlockerDict[blockerEnt] = data;
            return;
        }

        var center = aabb.Value.Center;
        var vec = center - pos.Position;
        var list = new List<Vector2>
            { aabb.Value.BottomLeft, aabb.Value.BottomRight, aabb.Value.TopLeft, aabb.Value.TopRight };
        foreach (var point in list)
        {
            var angle = AngleBetween(point - pos.Position, vec);
            minAngleTheta = MathF.Min(minAngleTheta, angle);
            maxAngleTheta = MathF.Max(maxAngleTheta, angle);
        }

        if (Math.Abs(minAngleTheta - maxAngleTheta) < 0.001f)
        {
            SoundBlockerDict.Remove(blockerEnt);
            return;
        }

        GatherAudio(blockerEnt, pos.Position, data, new Vector2(minAngleTheta, maxAngleTheta));
        SoundBlockerDict[blockerEnt] = data;
    }

    private void ResetBlockerOnGrid(Entity<MapGridComponent> grid, EntityUid blocker, MapCoordinates blockerPos, Vector2i? oldIndices)
    {
        var indices = _map.TileIndicesFor(grid, blockerPos);
        AddOrRemoveBlocker(blocker, indices, true, true);

        if (oldIndices != null)
        {
            if (oldIndices.Value == indices)
                return;

            AddOrRemoveBlocker(blocker, oldIndices.Value, false, true);
        }
    }

    private Box2Rotated? CalculateAABB(Entity<FixturesComponent?, TransformComponent?> blocker)
    {
        if (!Resolve(blocker, ref blocker.Comp1, ref blocker.Comp2, false))
            return null;

        Box2? aabb = null;
        var transform = _physics.GetPhysicsTransform(blocker.Owner, blocker.Comp2);
        foreach (var (_, value) in blocker.Comp1.Fixtures)
        {
            if ((value.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                continue;

            var newBox = value.Shape is PhysShapeAabb physShapeAabb
                ? physShapeAabb.LocalBounds
                : value.Shape.ComputeAABB(transform, 0);
            aabb = aabb?.Union(newBox) ?? newBox;
        }

        return aabb == null
            ? null
            : new Box2Rotated(aabb.Value, _xform.GetWorldRotation(blocker.Comp2), aabb.Value.Center);
    }

    private void GatherAudio(Entity<SoundBlockerComponent> blocker,
        Vector2 pos,
        List<(Vector2, Entity<AudioComponent>)> data,
        Vector2? minMaxAngle)
    {
        _audioToRemove.Clear();
        foreach (var (key, value) in ReverseSoundBlockerDict)
        {
            if (AudioPosDict.ContainsKey(key))
                continue;

            if (!Exists(key))
            {
                _audioToRemove.Add(key);
                continue;
            }

            var audioPos = _xform.GetWorldPosition(key);
            var dir = (audioPos - pos).Normalized();
            var theta = dir.ToAngle().Reduced().Theta;

            if (minMaxAngle != null && (theta < minMaxAngle.Value.X || theta > minMaxAngle.Value.Y))
                continue;

            if (data.FirstOrNull(x => x.Item2 == key) is { } tuple)
            {
                data.Remove(tuple);
                tuple.Item1 = dir;
                data.Add(tuple);
            }
            else
                data.Add((dir, key));

            value.Add(blocker);
        }

        foreach (var remove in _audioToRemove)
        {
            ReverseSoundBlockerDict.Remove(remove);
        }

        _audioToRemove.Clear();
    }

    private void ReCalculateAudioMuffle(EntityUid player,
        Entity<AudioComponent?> audio,
        MapCoordinates audioPos,
        float? volume = null,
        bool reset = true)
    {
        if (!Resolve(audio, ref audio.Comp, false))
            return;

        Entity<AudioComponent> audioEnt = (audio, audio.Comp);

        var playerPos = _xform.GetMapCoordinates(player);

        if (audioPos.MapId != playerPos.MapId)
        {
            RemoveAudioMuffle(audio);
            return;
        }

        if (TryFindCommonPlayerGrid(playerPos, audioPos) is { } grid)
        {
            var audioIndices = _map.TileIndicesFor(grid, audioPos);
            if (AudioPosDict.TryGetValue(audioEnt, out var oldIndices))
            {
                if (audioIndices == oldIndices)
                {
                    ResetAudioMuffle(audio, volume, reset, true);
                    return;
                }

                if (ReverseAudioPosDict.TryGetValue(oldIndices, out var oldSet))
                {
                    oldSet.Remove(audioEnt);
                    if (oldSet.Count == 0)
                        ReverseAudioPosDict.Remove(oldIndices);
                }
            }

            AudioPosDict[audioEnt] = audioIndices;
            if (ReverseAudioPosDict.TryGetValue(audioIndices, out var audioSet))
                audioSet.Add(audioEnt);
            else
                ReverseAudioPosDict[audioIndices] = new HashSet<Entity<AudioComponent>> {audioEnt};

            ResetAudioMuffle(audio, volume, reset, true);
            return;
        }

        if (audioPos.Position.EqualsApprox(playerPos.Position))
        {
            RemoveAudioMuffle(audio, false);
            return;
        }

        if (AudioPosDict.Remove(audioEnt, out var audioCoords) &&
            ReverseAudioPosDict.TryGetValue(audioCoords, out var set))
        {
            set.Remove(audioEnt);
            if (set.Count == 0)
                ReverseAudioPosDict.Remove(audioCoords);
        }

        var dir = audioPos.Position - playerPos.Position;
        var len = dir.Length();
        var norm = dir / len;

        var range = MathF.Min(len, AudioRange);

        var ray = new CollisionRay(playerPos.Position, norm, (int) CollisionGroup.Impassable);
        var result = _physics.IntersectRay(playerPos.MapId, ray, range, player, false);

        var hashSet = new HashSet<Entity<SoundBlockerComponent>>();
        foreach (var hitResult in result)
        {
            var potentialBlocker = hitResult.HitEntity;

            if (potentialBlocker == audio.Owner)
                continue;

            if (!_blockerQuery.TryComp(potentialBlocker, out var blockerComp))
                continue;

            if (_xform.GetWorldPosition(potentialBlocker).EqualsApprox(audioPos.Position))
                continue;

            Entity<SoundBlockerComponent> blockerEnt = (potentialBlocker, blockerComp);

            var coords = _xform.GetMapCoordinates(potentialBlocker);
            if (TryFindCommonPlayerGrid(playerPos, coords) is { } blockerGrid)
            {
                var indices = _map.TileIndicesFor(blockerGrid, coords);
                if (!BlockerIndicesDict.TryGetValue(blockerEnt, out var blockerIndices) ||
                    blockerIndices != indices)
                {
                    BlockerIndicesDict[blockerEnt] = indices;
                    AddOrRemoveBlocker(potentialBlocker, indices, true, true);
                }
            }

            hashSet.Add(blockerEnt);
            var blockerList = SoundBlockerDict.GetOrNew(blockerEnt);
            foreach (var tuple in blockerList)
            {
                if (tuple.Item2.Owner != audio.Owner)
                    continue;

                blockerList.Remove(tuple);
                break;
            }
        }

        if (hashSet.Count == 0)
            ReverseSoundBlockerDict.Remove(audioEnt);
        else
            ReverseSoundBlockerDict[audioEnt] = hashSet;

        ResetAudioMuffle(audio, volume, reset, true);
    }

    public Entity<MapGridComponent>? TryFindCommonPlayerGrid(MapCoordinates pos, MapCoordinates other)
    {
        if (ResolvePlayerGrid(pos) is { } grid &&
            _mapManager.TryFindGridAt(other, out var gridB, out _) && grid.Owner == gridB)
            return grid;

        return null;
    }

    private void RemoveAudioMuffle(Entity<AudioComponent?> audio, bool removeFromDict = true)
    {
        if (!Resolve(audio, ref audio.Comp, false))
            return;

        Entity<AudioComponent> audioEnt = (audio, audio.Comp);

        if (removeFromDict)
        {
            AudioVolumeDict.Remove(audioEnt);
            if (AudioPosDict.Remove(audioEnt, out var pos) &&
                ReverseAudioPosDict.TryGetValue(pos, out var audioSet))
            {
                audioSet.Remove(audioEnt);
                if (audioSet.Count == 0)
                    ReverseAudioPosDict.Remove(pos);
            }
        }

        if (!ReverseSoundBlockerDict.Remove(audioEnt, out var blockers))
            return;

        foreach (var blocker in blockers)
        {
            if (!SoundBlockerDict.TryGetValue(blocker, out var data))
                continue;

            foreach (var audioData in data)
            {
                if (audioData.Item2 != audioEnt)
                    continue;

                data.Remove(audioData);
                break;
            }

            if (data.Count > 0)
                continue;

            SoundBlockerDict.Remove(blocker);
        }
    }

    private void ResetAudioMuffle(Entity<AudioComponent?> audio, float? volume, bool reset = true, bool ignoreNonPlaying = false)
    {
        if (!Exists(audio) || !Resolve(audio, ref audio.Comp, false))
            return;

        Entity<AudioComponent> audioEnt = (audio, audio.Comp);

        if (audio.Comp.Global)
            return;

        if (!ignoreNonPlaying)
        {
            if (!audio.Comp.Playing)
                return;

            var offset = ((audio.Comp.PauseTime ?? _timing.CurTime) - audio.Comp.AudioStart).TotalSeconds;
            if (offset < SharedAudioSystem.AudioDespawnBuffer)
                return;
        }

        if (volume != null && !float.IsInfinity(volume.Value))
            AudioVolumeDict[audioEnt] = volume.Value;
        else if (!AudioVolumeDict.ContainsKey(audioEnt))
        {
            if (float.IsInfinity(audio.Comp.Params.Volume))
                return;
            AudioVolumeDict.Add(audioEnt, audio.Comp.Params.Volume);
        }

        if (!reset || audio.Comp.State == AudioState.Stopped || !audio.Comp.Loaded ||
            ResolvePlayer() is not { } player)
            return;

        var muffleLevel = 0f;
        var xform = Transform(player);
        var playerPos = _xform.GetMapCoordinates(player, xform);

        if (ResolvePlayerGrid(playerPos) is { } grid && AudioPosDict.TryGetValue(audioEnt, out var pos) &&
            TileDataDict.TryGetValue(pos, out var tileData))
        {
            var playerIndices = _map.TileIndicesFor(grid, playerPos);
            var playerDist = (float) ManhattanDistance(pos, playerIndices);
            if (TileDataDict.TryGetValue(playerIndices, out var playerTile) && playerTile.Previous != null)
                playerDist += (xform.Coordinates.Position - playerTile.Previous.Value).Length() - 1f;
            muffleLevel = tileData.TotalCost + (playerDist - AudioRange) / 4f - GetTotalTileCost(pos);
        }
        else if (ReverseSoundBlockerDict.TryGetValue(audioEnt, out var data))
        {
            _blockersToRemove.Clear();
            foreach (var blocker in data)
            {
                if (!TryGetBlockerCost(blocker.AsNullable(), out var cost))
                {
                    _blockersToRemove.Add(blocker);
                    continue;
                }

                muffleLevel += cost;
            }

            foreach (var remove in _blockersToRemove)
            {
                SoundBlockerDict.Remove(remove);
                BlockerIndicesDict.Remove(remove);
                data.Remove(remove);
            }

            _blockersToRemove.Clear();

            if (data.Count == 0)
                ReverseSoundBlockerDict.Remove(audioEnt);
        }
        else if ((_xform.GetWorldPosition(audio) - playerPos.Position).Length() <= AudioRange)
            muffleLevel = 0f;
        else
            muffleLevel = 16f;

        SetVolume(audio, volume ?? AudioVolumeDict[audioEnt], muffleLevel);
    }

    private void ResetAllRaycastAudio()
    {
        _audioToRemove.Clear();
        foreach (var audio in ReverseSoundBlockerDict.Keys)
        {
            if (!Exists(audio))
            {
                _audioToRemove.Add(audio);
                continue;
            }

            ResetAudioMuffle(audio.AsNullable(), null);
        }

        foreach (var remove in _audioToRemove)
        {
            AudioVolumeDict.Remove(remove);
            if (!ReverseSoundBlockerDict.Remove(remove, out var blockers))
                continue;

            foreach (var blocker in blockers)
            {
                if (!SoundBlockerDict.TryGetValue(blocker, out var list))
                    continue;

                foreach (var tuple in list)
                {
                    if (tuple.Item2 != remove)
                        continue;

                    list.Remove(tuple);
                    break;
                }
            }
        }

        _audioToRemove.Clear();
    }

    private void ResetAllPosAudio(HashSet<Vector2i>? toUpdate = null)
    {
        _audioToRemove.Clear();
        foreach (var (audio, pos) in AudioPosDict)
        {
            if (!Exists(audio))
            {
                _audioToRemove.Add(audio);
                continue;
            }

            if (toUpdate == null || toUpdate.Contains(pos))
                ResetAudioMuffle(audio.AsNullable(), null);
        }

        foreach (var remove in _audioToRemove)
        {
            AudioVolumeDict.Remove(remove);
            if (!AudioPosDict.Remove(remove, out var pos) ||
                !ReverseAudioPosDict.TryGetValue(pos, out var audioSet))
                continue;

            audioSet.Remove(remove);
            if (audioSet.Count == 0)
                ReverseAudioPosDict.Remove(pos);
        }

        _audioToRemove.Clear();
    }

    private void ResetAudioOnPos(Vector2i pos)
    {
        if (!ReverseAudioPosDict.TryGetValue(pos, out var audioSet))
            return;

        _audioToRemove.Clear();
        foreach (var audio in audioSet)
        {
            if (!Exists(audio))
            {
                _audioToRemove.Add(audio);
                continue;
            }

            ResetAudioMuffle(audio.AsNullable(), null);
        }

        foreach (var remove in _audioToRemove)
        {
            AudioVolumeDict.Remove(remove);
            AudioPosDict.Remove(remove);
            audioSet.Remove(remove);
        }

        _audioToRemove.Clear();

        if (audioSet.Count == 0)
            ReverseAudioPosDict.Remove(pos);
    }

    public float GetTotalTileCost(Vector2i tile)
    {
        if (!ReverseBlockerIndicesDict.TryGetValue(tile, out var blockers))
            return 0f;

        var total = 0f;
        _blockersToRemove.Clear();
        foreach (var blocker in blockers)
        {
            if (!TryGetBlockerCost(blocker.AsNullable(), out var cost))
            {
                _blockersToRemove.Add(blocker);
                continue;
            }

            total += cost;
        }

        foreach (var remove in _blockersToRemove)
        {
            BlockerIndicesDict.Remove(remove);
            blockers.Remove(remove);
        }

        _blockersToRemove.Clear();

        if (blockers.Count == 0)
            ReverseBlockerIndicesDict.Remove(tile);

        return total;
    }

    public bool TryGetBlockerCost(Entity<SoundBlockerComponent?> blocker, out float cost)
    {
        cost = 0f;
        if (!Exists(blocker) || !Resolve(blocker.AsNullable(), ref blocker.Comp, false))
            return false;

        cost = GetBlockerCost(blocker.Comp);
        return true;
    }

    public float GetBlockerCost(SoundBlockerComponent blocker)
    {
        var percent = MathF.Max(blocker.SoundBlockPercent, 0f);
        return percent > 0.99f ? 400f : -(1 / (percent - 1)) * 4 - 4;
    }

    private void SetVolume(Entity<AudioComponent?> audio, float volume, float muffleLevel)
    {
        if (TerminatingOrDeleted(audio))
            return;

        if (!Resolve(audio, ref audio.Comp, false))
            return;

        switch (muffleLevel)
        {
            case <= 0f:
                break;
            case >= 16f:
                volume = -100f;
                break;
            default:
                var gain = SharedAudioSystem.VolumeToGain(volume) / MathF.Pow(muffleLevel / 16 + 1, 4f);
                volume = SharedAudioSystem.GainToVolume(gain);
                break;
        }

        _audio.SetVolume(audio, volume, audio);
    }

    private float AngleBetween(Vector2 a, Vector2 b)
    {
        var div = a.Length() * b.Length();
        return MathHelper.CloseToPercent(div, 0f) ? 0f : MathF.Acos(Vector2.Dot(a, b) / div);
    }
}
