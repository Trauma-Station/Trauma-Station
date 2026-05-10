// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
    [Dependency] private readonly ProfManager _prof = default!;

    private List<Vector2i> _empty = new();
    private List<byte> _badIds = new();
    private Dictionary<EntProtoId, byte> _mapping = new();
    private Stopwatch _stopwatch = new();

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
        if (GetChunk(ent, out var index, create: true) is not {} chunk)
        {
            var xform = Transform(ent);
            if (xform.GridUid is {} grid)
                Log.Error($"Failed to create a chunk for area {ToPrettyString(ent)} on grid {ToPrettyString(grid)}!");
            else if (xform.MapID != MapId.Nullspace) // ignore for entity spawn menu
                PredictedDel(ent.Owner); // no spawning areas in space...
            return;
        }

        var added = Deleted(chunk.Areas[index]);
        chunk.Areas[index] = ent;
        if (added)
            chunk.AreaCount++;
    }

    private void OnShutdown(Entity<AreaComponent> ent, ref ComponentShutdown args)
    {
        if (GetChunk(ent, out var index) is not {} chunk)
            return;

        DebugTools.Assert(chunk.Areas[index] == ent.Owner, $"{ToPrettyString(ent)} was not at the right area!");
        chunk.Areas[index] = EntityUid.Invalid;
        chunk.AreaCount--;
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
        var chunks = ent.Comp.Chunks.Count;
        if (chunks == 0)
            return; // empty...

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
        _stopwatch.Restart();
        using (_prof.Group("Areas"))
        {
            foreach (var (indices, chunk) in ent.Comp.Chunks)
            {
                // centered so floating point errors can only occur at absurdly large map sizes
                var offset = new Vector2(indices.X * size + 0.5f, indices.Y * size + 0.5f);
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

        var time = _stopwatch.Elapsed;
        Log.Debug($"Loaded areas for {ToPrettyString(ent)} in {time} ({chunks} x {time / chunks})");
    }

    private void LoadChunk(Entity<AreaGridComponent> ent, int size, Vector2 offset, AreaChunk chunk)
    {
        // only load areas if they were specified in the map
        if (string.IsNullOrEmpty(chunk.Data))
            return;

        var area = size * size;
        chunk.Areas = new EntityUid[area]; // it's null when loading from yml
        var map = ent.Comp.AreaMap;
        byte[] bytes = Convert.FromBase64String(chunk.Data);
        if (bytes.Length != area)
        {
            Log.Error($"Bytes of grid {ToPrettyString(ent)} chunk at {offset} had bad length {bytes.Length}, expected {area}!");
            return;
        }

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
            var spawned = PredictedSpawnAtPosition(id, coords); // predicted map loading..?
            if (chunk.Areas[i] != spawned)
            {
                Log.Error($"Area {ToPrettyString(spawned)} at {local} of grid {ToPrettyString(ent)} did not have working setup logic!");
            }
            chunk.Areas[i] = spawned;
        }
    }

    private void SaveGrid(AreaGridComponent areas)
    {
        // clean up any empty chunks
        _empty.Clear();
        foreach (var (indices, chunk) in areas.Chunks)
        {
            PruneDeletedAreas(chunk);
            if (chunk.AreaCount == 0)
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
            foreach (var uid in chunk.Areas)
            {
                // TODO: might want to cache the id somewhere..?
                if (!uid.IsValid() || Prototype(uid)?.ID is not {} id)
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
    /// If <c>create</c> is true, it will create a chunk if it doesn't exist.
    /// </summary>
    private AreaChunk? GetChunk(EntityUid area, out int index, bool create = false)
    {
        var xform = Transform(area);
        index = 0;
        if (GetGrid((area, xform)) is not {} grid)
            return null;

        return GetChunk(grid.AsNullable(), xform.Coordinates.Position, out index, create);
    }

    /// <summary>
    /// Gets an area chunk from a grid and local position.
    /// If <c>create</c> is true, it will create a chunk if it doesn't exist.
    /// </summary>
    private AreaChunk? GetChunk(Entity<AreaGridComponent?> grid, Vector2 pos, out int index, bool create = false)
    {
        index = 0;
        if (!_query.Resolve(grid, ref grid.Comp, false))
            return null;

        // calculate index for the chunk inside the grid
        var size = grid.Comp.ChunkSize;
        var chunks = grid.Comp.Chunks;
        var indices = new Vector2i((int) MathF.Floor(pos.X / size), (int) MathF.Floor(pos.Y / size));
        // calculate index inside the chunk
        var x = (int) MathF.Floor(pos.X - indices.X * size);
        var y = (int) MathF.Floor(pos.Y - indices.Y * size);
        DebugTools.Assert(x >= 0 && x < size);
        DebugTools.Assert(x >= 0 && y < size);
        index = x + y * size;
        if (chunks.TryGetValue(indices, out var chunk))
            return chunk;

        if (!create)
            return null;

        return chunks[indices] = new()
        {
            Areas = new EntityUid[size * size]
        };
    }

    /// <summary>
    /// Try to get an area at a given grid-local position on a grid.
    /// </summary>
    public EntityUid? GetArea(Entity<AreaGridComponent?> grid, Vector2 pos)
    {
        if (!_query.Resolve(grid, ref grid.Comp, false) ||
            GetChunk(grid, pos, out var index) is not {} chunk)
            return null;

        var area = chunk.Areas[index];
        return area.IsValid() ? area : null;
    }

    private void BuildChunk(int size, Vector2 offset, AreaChunk chunk)
    {
        var bytes = new byte[size * size];
        foreach (var uid in chunk.Areas)
        {
            if (!uid.IsValid() || Prototype(uid)?.ID is not {} id)
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

    private void PruneDeletedAreas(AreaChunk chunk)
    {
        for (int i = 0; i < chunk.Areas.Length; i++)
        {
            var area = chunk.Areas[i];
            if (area.IsValid() && Deleted(area))
            {
                Log.Warning($"Found deleted area {area} inside a chunk at index {i}! It should have been cleaned up in ComponentShutdown");
                chunk.Areas[i] = EntityUid.Invalid;
                chunk.AreaCount--;
            }
        }
    }
}
