using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Maps;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;
using ChunkIndicesEnumerator = Robust.Shared.Map.Enumerators.ChunkIndicesEnumerator;

namespace Content.Shared.Decals;

// Trauma - completely rewrote decals to be entity based
public abstract partial class SharedDecalSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager PrototypeManager = default!;
    [Dependency] protected IMapManager MapManager = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] protected SharedMapSystem Map = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityQuery<DecalComponent> _query = default!;
    [Dependency] protected EntityQuery<DecalGridComponent> GridQuery = default!;
    [Dependency] protected EntityQuery<MapGridComponent> MapGridQuery = default!;

    public static readonly EntProtoId DecalEntity = "Decal";

    // Note that this constant is effectively baked into all map files, because of how they save the grid decal component.
    // So if this ever needs changing, the maps need converting.
    public const int ChunkSize = 32;
    public static Vector2i GetChunkIndices(Vector2 coordinates) => (coordinates / ChunkSize).Floored();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<DecalGridComponent, ComponentStartup>(OnGridStartup);
        SubscribeLocalEvent<DecalComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DecalComponent, MoveEvent>(OnMove);
    }

    private void OnGridInitialize(GridInitializeEvent msg)
    {
        EnsureComp<DecalGridComponent>(msg.EntityUid);
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (!GridQuery.TryComp(args.Entity, out var grid))
            return;

        foreach (var change in args.Changes)
        {
            if (!_turf.IsSpace(change.NewTile))
                continue;

            if (!grid.ChunkCollection.ChunkCollection.TryGetValue(change.ChunkIndex, out var chunk))
                continue;

            foreach (var decal in chunk.Decals)
            {
                if (GetChunkIndices(decal.Coordinates) == change.GridIndices)
                    PredictedQueueDel(decal.Ent.Owner);
            }
        }
    }

    private void OnGridStartup(EntityUid uid, DecalGridComponent component, ComponentStartup args)
    {
        foreach (var (indices, chunk) in component.ChunkCollection.ChunkCollection)
        {
            // spawn decal entities from the map's data
            foreach (var data in chunk.Decals)
            {
                AddDecal(uid, data, chunk, indices);
            }
        }
    }

    private void OnStartup(Entity<DecalComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Data == default)
            return; // not initialized yet

        // dont want clients to detach them for performance
        _meta.AddFlag(ent.Owner, MetaDataFlags.Undetachable);

        ent.Comp.Data.Ent = ent;
        TryAddToChunk(ent, Transform(ent));
    }

    private void OnMove(Entity<DecalComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.Data == default)
            return;

        var oldGrid = args.OldPosition.EntityId;
        var indices = ent.Comp.Chunk;
        if (oldGrid.IsValid())
            GetGridChunk(oldGrid, indices)?.Decals.Remove(ent.Comp.Data);
        TryAddToChunk(ent, args.Component);
    }

    private void TryAddToChunk(Entity<DecalComponent> ent, TransformComponent xform)
    {
        if (xform.GridUid is { } newGrid)
            GetGridChunk(newGrid, ent.Comp.Chunk, true)?.Decals.Add(ent.Comp.Data);
        else
            PredictedQueueDel(ent); // can't have decals in space
    }

    protected DecalChunk? GetChunk(Entity<DecalComponent> decal, bool create = false)
        => Transform(decal).GridUid is { } grid
            ? GetGridChunk(grid, decal.Comp.Chunk, create)
            : null;

    protected DecalChunk? GetGridChunk(EntityUid gridUid, Vector2i indices, bool create = false)
    {
        if (!GridQuery.TryComp(gridUid, out var grid))
            return null;

        var chunks = grid.ChunkCollection.ChunkCollection;
        if (chunks.TryGetValue(indices, out var chunk))
            return chunk;

        if (!create)
            return null;

        return chunks[indices] = new();
    }

    protected Dictionary<Vector2i, DecalChunk>? ChunkCollection(EntityUid gridEuid, DecalGridComponent? comp = null)
    {
        if (!GridQuery.Resolve(gridEuid, ref comp, false))
            return null;

        return comp.ChunkCollection.ChunkCollection;
    }

    public bool TryAddDecal(string id, EntityCoordinates coordinates, out EntityUid decalId, Color? color = null, Angle? rotation = null, int zIndex = 0, bool cleanable = false)
    {
        rotation ??= Angle.Zero;
        var decal = new Decal(coordinates.Position, id, color, rotation.Value, zIndex, cleanable);

        return TryAddDecal(decal, coordinates, out decalId);
    }

    public bool TryAddDecal(Decal decal, EntityCoordinates coordinates, out EntityUid decalId)
    {
        decalId = EntityUid.Invalid;

        if (!PrototypeManager.HasIndex<DecalPrototype>(decal.Id))
        {
            Log.Error($"Tried to spawn decal with invalid prototype {decal.Id} at {coordinates}!");
            return false;
        }

        var gridId = Xform.GetGrid(coordinates);
        if (!MapGridQuery.TryComp(gridId, out var grid))
            return false;

        if (_turf.IsSpace(Map.GetTileRef(gridId.Value, grid, coordinates)))
            return false;

        if (!GridQuery.TryComp(gridId, out var comp))
            return false;

        var chunkIndices = GetChunkIndices(decal.Coordinates);
        var chunk = comp.ChunkCollection.ChunkCollection.GetOrNew(chunkIndices);
        decalId = AddDecal(gridId.Value, decal, chunk, chunkIndices);
        return true;
    }

    private EntityUid AddDecal(EntityUid grid, Decal decal, DecalChunk chunk, Vector2i chunkIndices)
    {
        // maps and all code that adds decals have coords as the bottom left of the tile, move it to the center
        var coords = new EntityCoordinates(grid, decal.Coordinates + new Vector2(0.5f, 0.5f));
        var decalId = PredictedSpawnAtPosition(DecalEntity, coords);
        Xform.SetLocalRotation(decalId, decal.Angle);
        chunk.Decals.Add(decal);

        var decalComp = _query.Comp(decalId);
        decal.Ent = (decalId, decalComp);
        decalComp.Data = decal;
        decalComp.Chunk = chunkIndices;
        Dirty(decalId, decalComp);

        return decalId;
    }

    /// <summary>
    /// Get all decals on a given grid in range of some position on it, that optionally match a delegate.
    /// This will not work across chunk boundaries, so keep distances small to make this less noticable.
    /// </summary>
    public HashSet<Entity<DecalComponent>> GetDecalsInRange(EntityUid gridId, Vector2 position, float distance = 0.75f, Func<Decal, bool>? validDelegate = null)
    {
        var decalIds = new HashSet<Entity<DecalComponent>>();
        var chunkCollection = ChunkCollection(gridId);
        var chunkIndices = GetChunkIndices(position);
        if (chunkCollection == null || !chunkCollection.TryGetValue(chunkIndices, out var chunk))
            return decalIds;

        var dist2 = distance * distance;
        foreach (var decal in chunk.Decals)
        {
            var ent = decal.Ent;
            if (!TryComp(ent, out TransformComponent? xform))
            {
                Log.Error($"Deleted decal {ent} found in chunk {chunkIndices} of {ToPrettyString(gridId)}!");
                continue;
            }

            var decalPos = xform.Coordinates.Position;
            if ((position - decalPos).LengthSquared() > dist2)
                continue;

            if (validDelegate == null || validDelegate(decal))
            {
                decalIds.Add(ent);
            }
        }

        return decalIds;
    }

    public HashSet<Entity<DecalComponent>> GetDecalsIntersecting(EntityUid gridUid, Box2 bounds, DecalGridComponent? component = null)
    {
        var decalIds = new HashSet<Entity<DecalComponent>>();
        var chunkCollection = ChunkCollection(gridUid, component);

        if (chunkCollection == null)
            return decalIds;

        var chunks = new ChunkIndicesEnumerator(bounds, ChunkSize);

        while (chunks.MoveNext(out var chunkOrigin))
        {
            if (!chunkCollection.TryGetValue(chunkOrigin.Value, out var chunk))
                continue;

            foreach (var decal in chunk.Decals)
            {
                if (!bounds.Contains(decal.Coordinates))
                    continue;

                decalIds.Add(decal.Ent);
            }
        }

        return decalIds;
    }
}

/// <summary>
///     Sent by clients to request that a decal is placed on the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestDecalPlacementEvent : EntityEventArgs
{
    public Decal Decal;
    public NetCoordinates Coordinates;

    public RequestDecalPlacementEvent(Decal decal, NetCoordinates coordinates)
    {
        Decal = decal;
        Coordinates = coordinates;
    }
}

[Serializable, NetSerializable]
public sealed class RequestDecalRemovalEvent : EntityEventArgs
{
    public NetCoordinates Coordinates;

    public RequestDecalRemovalEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
