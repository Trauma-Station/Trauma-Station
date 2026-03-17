// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Handles map deserializing and serializing of areas.
/// Instead of entire entities which is a huge waste of text, basically do the same thing as tiles.
/// Then when loading the map spawn the entities by reading the areamap.
/// Only real difference is areamap is stored on the grid instead of root save yml, it's not really doable with current RT.
/// Only 256 area prototypes are supported.
/// </summary>
public sealed class MapAreaSystem : EntitySystem
{
    [Dependency] private readonly EntityQuery<AreaGridComponent> _query = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private List<Vector2i> _empty = new();
    private List<byte> _badIds = new();
    private Dictionary<EntProtoId, byte> _mapping = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AreaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AreaComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<GridAddEvent>(OnGridAdd);

        SubscribeLocalEvent<AreaGridComponent, ComponentStartup>(OnGridStartup);

        SubscribeLocalEvent<BeforeSerializationEvent>(OnBeforeSave);
    }

    private void OnStartup(Entity<AreaComponent> ent, ref ComponentStartup args)
    {
        if (GetChunk(ent, create: true) is {} chunk)
            chunk.Areas.Add(ent);
    }

    private void OnShutdown(Entity<AreaComponent> ent, ref ComponentShutdown args)
    {
        if (GetChunk(ent) is {} chunk)
            chunk.Areas.Remove(ent);
    }

    private void OnGridAdd(GridAddEvent args)
    {
        EnsureComp<AreaGridComponent>(args.EntityUid);
    }

    private void OnBeforeSave(BeforeSerializationEvent args)
    {
        // all because need paused grids for mapping lol
        var query = AllEntityQuery<AreaGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var areas, out var xform))
        {
            // don't care if this grid isn't being saved
            if (!args.MapIds.Contains(xform.MapID))
                continue;

            try
            {
                SaveGrid(areas);
            }
            catch (Exception e)
            {
                Log.Error($"Caught exception while saving areas for grid {ToPrettyString(uid)}: {e}");
            }
        }
    }

    private void OnGridStartup(Entity<AreaGridComponent> ent, ref ComponentStartup args)
    {
        var size = ent.Comp.ChunkSize;
        // verify that none of the areas used got removed, skip any that were
        foreach (var (mapped, id) in ent.Comp.AreaMap)
        {
            if (_proto.HasIndex(id))
                continue;

            Log.Error($"Area {id} ({mapped}) used by grid {ToPrettyString(ent)} does not exist!");
            _badIds.Add(mapped);
        }

        foreach (var mapped in _badIds)
        {
            ent.Comp.AreaMap.Remove(mapped);
        }

        // now spawn all the areas it used
        Log.Debug($"Loading {ent.Comp.AreaMap.Count} unique areas for {ToPrettyString(ent)}");
        foreach (var (indices, chunk) in ent.Comp.Chunks.ToList()) // copy since it may modify chunks by spawning areas...
        {
            var offset = new Vector2(indices.X * size, indices.Y * size);
            try
            {
                LoadChunk(ent, size, offset, chunk);
            }
            catch (Exception e)
            {
                Log.Error($"Caught exception while loading areas for grid {ToPrettyString(ent)} @ {indices}: {e}");
            }
        }
    }

    private void LoadChunk(Entity<AreaGridComponent> ent, int size, Vector2 offset, AreaChunk chunk)
    {
        // only load areas if they were specified in the map
        if (string.IsNullOrEmpty(chunk.Data))
            return;

        var map = ent.Comp.AreaMap;
        byte[] bytes = Convert.FromBase64String(chunk.Data);
        var area = size * size;
        DebugTools.Assert(bytes.Length == area, $"Bytes had bad length {bytes.Length}, expected {area}");

        for (int i = 0; i < area; i++)
        {
            var mapped = bytes[i];
            if (mapped == 0)
                continue; // empty, no area here

            if (!map.TryGetValue(mapped, out var id))
                continue; // invalid id, skip it

            var x = i % size;
            var y = i / size;
            var local = new Vector2(offset.X + x, offset.Y + y);
            var coords = new EntityCoordinates(ent, local);
            PredictedSpawnAtPosition(id, coords); // predicted map loading..?
        }
    }

    private void SaveGrid(AreaGridComponent areas)
    {
        // clean up any empty chunks
        _empty.Clear();
        foreach (var (indices, chunk) in areas.Chunks)
        {
            if (chunk.Areas.Count == 0)
                _empty.Add(indices);
        }

        foreach (var indices in _empty)
        {
            areas.Chunks.Remove(indices);
        }

        // add any new areas to the id table, and make the inverse mapping
        _mapping.Clear();
        foreach (var (i, id) in areas.AreaMap)
        {
            _mapping[id] = i;
        }
        foreach (var chunk in areas.Chunks.Values)
        {
            chunk.Areas.RemoveWhere(uid => Deleted(uid));
            foreach (var uid in chunk.Areas)
            {
                // TODO: might want to cache the id somewhere..?
                if (Prototype(uid)?.ID is not {} id)
                    continue;

                if (_mapping.ContainsKey(id))
                    continue; // already in the map

                var i = ++areas.LastMapping; // pre-increment so first id mapping is 1 not 0
                _mapping[id] = i;
                areas.AreaMap[i] = id;
            }
        }
        // unused areas are not removed, otherwise it would be possible to overflow the 256 area limit
        // by adding + removing the same area prototype over and over

        // build the string for each chunk now
        var size = (int) areas.ChunkSize;
        foreach (var (indices, chunk) in areas.Chunks)
        {
            var offset = new Vector2(indices.X * size, indices.Y * size);
            BuildChunk(size, offset, chunk);
        }
    }

    private Entity<AreaGridComponent>? GetGrid(Entity<TransformComponent> area)
    {
        if (area.Comp.GridUid is not {} grid)
            return null;

        if (_query.TryComp(grid, out var comp))
            return (grid, comp);

        Log.Error($"Grid {ToPrettyString(grid)} for area {ToPrettyString(area)} was missing AreaGridComponent!");
        return null;
    }

    /// <summary>
    /// Gets an area chunk from an area's grid.
    /// If <c>create</c> is true, it will creating a chunk if it doesn't exist.
    /// </summary>
    public AreaChunk? GetChunk(EntityUid area, bool create = false)
    {
        var xform = Transform(area);
        if (GetGrid((area, xform)) is not {} grid)
            return null;

        var size = grid.Comp.ChunkSize;
        var chunks = grid.Comp.Chunks;
        var pos = xform.Coordinates.Position;
        var indices = new Vector2i((int) MathF.Floor(pos.X / size), (int) MathF.Floor(pos.Y / size));
        if (chunks.TryGetValue(indices, out var chunk))
            return chunk;

        if (!create)
            return null;

        return chunks[indices] = new();
    }

    private void BuildChunk(int size, Vector2 offset, AreaChunk chunk)
    {
        var bytes = new byte[size * size];
        foreach (var uid in chunk.Areas)
        {
            // these should always exist from ComponentShutdown removing it from the chunk.
            if (Prototype(uid)?.ID is not {} id)
                continue;

            var xform = Transform(uid);
            var local = xform.LocalPosition - offset;
            // areas shouldnt be moving...
            if (local.X < 0 || local.Y < 0 || local.X >= size || local.Y >= size)
            {
                DebugTools.Assert($"Area {ToPrettyString(uid)} was out of bounds @ {local} for chunk @ {offset} of {ToPrettyString(xform.GridUid)}!");
                continue;
            }

            var index = (int) local.X + (size * (int) local.Y);
            bytes[index] = _mapping[id];
        }

        chunk.Data = Convert.ToBase64String(bytes);
    }
}
