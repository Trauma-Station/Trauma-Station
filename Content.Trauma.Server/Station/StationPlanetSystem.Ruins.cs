// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Lavaland.Server.Procedural.Components;
using Content.Lavaland.Shared.Procedural.Prototypes;
using Content.Server.Decals;
using Content.Server.Procedural;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Station;

public sealed partial class StationPlanetSystem
{
    /*
      !!! Turn back while you still can !!!

      This is copy-pasted from lavaland to fit generic planet maps like Glacier.
      It uses lavaland prototypes because it's the only generic thing lavaland offers...

      I also added some extra documentation to methods.

      May god have mercy on the sins commited in this file.
    */

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tiledef = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;
    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;

    private readonly List<(Vector2i, Tile)> _tiles = new();

    /// <summary>
    /// Sets up all the ruins on the planet, if we have a pool defined.
    /// </summary>
    /// <param name="pool">The pool of ruins</param>
    /// <param name="map">The map entity</param>
    /// <param name="mapId">The map id of the map</param>
    private void SetupRuins(LavalandRuinPoolPrototype? pool, EntityUid map, MapId mapId)
    {
        if (pool == null)
            return; // nothing to spawn

        var seed = _random.Next();
        var random = new Random(seed);

        var usedSpace = GetMapBoundary(mapId);
        var coords = GetCoordinates(pool.RuinDistance, pool.MaxDistance);

        random.Shuffle(coords);

        // Load grid ruins
        Log.Debug($"Spawning {pool.GridRuins.Count} GridRuins on {ToPrettyString(map)} planet.");
        SetupHugeRuins(pool.GridRuins, map, random, ref coords, ref usedSpace);

        // Filter out coords from huge ruins, so they don't overlap with dungeon ruins
        coords = coords.Where(coord => !usedSpace.Any(box => box.Contains(coord))).ToList();

        // Load dungeon ruins
        // TODO: make it actual dungeons instead of spawning markers
        Log.Debug($"Spawning {pool.DungeonRuins.Count} DungeonRuins on {ToPrettyString(map)} planet.");
        SetupDungeonRuins(pool.DungeonRuins, map, random, ref coords, ref usedSpace);
    }

    /// <summary>
    /// Sets up non-dungeon ruins on the planet map
    /// </summary>
    /// <param name="ruins">A dictionary holding the ruin prototypes, and how many of them to spawn</param>
    /// <param name="map">The map entity</param>
    /// <param name="random">A <see cref="Random"/> with a seed set</param>
    /// <param name="coords">A set of coordinates</param>
    /// <param name="usedSpace">The space used by the map</param>
    private void SetupHugeRuins(
        Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort> ruins,
        EntityUid map,
        Random random,
        ref List<Vector2i> coords,
        ref List<Box2> usedSpace)
    {
        // Get and sort all ruins, because we can't sort dictionaries
        var list = GetGridRuinProtos(ruins);
        list.Sort((x, y) => x.Priority.CompareTo(y.Priority));

        // Place them down randomly
        foreach (var ruin in list)
        {
            var attempts = 0;
            while (!LoadGridRuin(ruin, map, random, ref usedSpace, ref coords))
            {
                attempts++;
                if (attempts <= ruin.SpawnAttempts)
                    continue;

                Log.Warning($"Failed to spawn GridRuin {ruin.ID} on {ToPrettyString(map)} surface! All {ruin.SpawnAttempts} attempts have failed.");
                break;
            }
        }
    }

    /// <summary>
    /// Sets up dungeon markers on the planet map
    /// </summary>
    /// <param name="ruins">A dictionary holidng the dungeon ruin prototypes, and how many of them to spawn</param>
    /// <param name="map">The map entity</param>
    /// <param name="random"><see cref="Random"/></param>
    /// <param name="coords">A set of coordinates</param>
    /// <param name="usedSpace">The space used by the map</param>
    private void SetupDungeonRuins(
        Dictionary<ProtoId<LavalandDungeonRuinPrototype>, ushort> ruins,
        EntityUid map,
        Random random,
        ref List<Vector2i> coords,
        ref List<Box2> usedSpace)
    {
        // Get and sort all dungeon ruins, because we can't sort dictionaries
        var list = GetDungeonRuinProtos(ruins);
        list.Sort((x, y) => x.Priority.CompareTo(y.Priority));

        // Place them down randomly
        foreach (var ruin in list)
        {
            var attempts = 0;
            while (!LoadDungeonRuin(ruin, map, random, ref usedSpace, ref coords))
            {
                attempts++;
                if (attempts > ruin.SpawnAttempts)
                    break;
            }
        }
    }

    /// <summary>
    /// Loads a <see cref="LavalandGridRuinPrototype"/> and places it on the map
    /// </summary>
    /// <param name="ruin">The grid ruin to load</param>
    /// <param name="map">The map entity</param>
    /// <param name="random"><see cref="Random"/></param>
    /// <param name="usedSpace">The map's used space</param>
    /// <param name="coords">A set of coordinates</param>
    /// <returns>True if the ruin was loaded, false otherwise</returns>
    private bool LoadGridRuin(
        LavalandGridRuinPrototype ruin,
        EntityUid map,
        Random random,
        ref List<Box2> usedSpace,
        ref List<Vector2i> coords)
    {
        if (coords.Count == 0)
            return false;

        var coord = random.Pick(coords);
        var mapXform = Transform(map);

        // Check if we already calculated that boundary before, and if we didn't then calculate it now
        if (!_loader.TryLoadGrid(mapXform.MapID, ruin.Path, out var spawnedBoundedGrid))
        {
            Log.Error($"Failed to load ruin {ruin.ID} onto dummy map, on stage of loading! AAAAA!!");
            return false;
        }

        // It's not useless!
        var spawned = spawnedBoundedGrid.Value;
        var ruinBox = spawnedBoundedGrid.Value.Comp.LocalAABB.Translated(coord);

        _transform.SetCoordinates(spawned, new EntityCoordinates(map, coord));

        // If any used boundary intersects with current boundary, return
        if (usedSpace.Any(used => used.Intersects(ruinBox)))
        {
            Log.Debug($"Ruin {ruin.ID} can't be placed on picked coordinates {coord.ToString()} on {ToPrettyString(map)} planet, skipping spawn.");
            Del(spawned); // nuh uh
            return false;
        }

        usedSpace.Add(ruinBox);
        coords.Remove(coord);
        if (ruin.PatchToPlanet)
            PatchToPlanet(spawned, (map, _mapGridQuery.Comp(map)), coord);
        else
        {
            var spawnedXForm = Transform(spawned);
            _metadata.SetEntityName(spawned, Loc.GetString(ruin.Name));
            _transform.SetParent(spawned, spawnedXForm, map);
            _transform.SetCoordinates(spawned,
                new EntityCoordinates(map, spawnedXForm.Coordinates.Position.Rounded()));

            var componentsToGrant = EnsureComp<LavalandGridGrantComponent>(spawned);
            foreach (var (key, comp) in ruin.ComponentsToGrant)
            {
                componentsToGrant.ComponentsToGrant[key] = comp;
            }
        }

        Log.Debug($"Successfully spawned ruin {ruin.ID} on {ToPrettyString(map)} planet surface at coordinates {coord.ToString()}");
        return true;
    }

    /// <summary>
    /// Loads a <see cref="LavalandDungeonRuinPrototype"/> and places it's marker on the map
    /// </summary>
    /// <param name="ruin">The dungeon ruin to load</param>
    /// <param name="map">The map entity</param>
    /// <param name="random"><see cref="Random"/></param>
    /// <param name="usedSpace">The map's used space</param>
    /// <param name="coords">A set of coordinates</param>
    /// <returns>True if the ruin was loaded, false otherwise</returns>
    private bool LoadDungeonRuin(
        LavalandDungeonRuinPrototype ruin,
        EntityUid map,
        Random random,
        ref List<Box2> usedSpace,
        ref List<Vector2i> coords)
    {
        if (coords.Count == 0)
            return false;

        var coord = random.Pick(coords);
        var box = Box2.CentredAroundZero(ruin.Boundary);
        var ruinBox = box.Translated(coord);

        // If any used boundary intersects with current boundary, return
        if (usedSpace.Any(used => used.Intersects(ruinBox)))
        {
            Log.Debug("Ruin can't be placed on it's coordinates, skipping spawn");
            return false;
        }

        // Spawn the marker
        Spawn(ruin.SpawnedMarker, new EntityCoordinates(map, coord));

        usedSpace.Add(ruinBox);
        coords.Remove(coord);
        return true;
    }

    /// <summary>
    /// Delets the grid and spawns all it's entities/decals/tiles into a given position with an offset
    /// </summary>
    /// <param name="grid">The grid to take the entities/decals/tiles from</param>
    /// <param name="map">The map to put the entities/decals/tiles on</param>
    /// <param name="offset">A given offset to adjust the position of entities/decals/tiles</param>
    private void PatchToPlanet(Entity<MapGridComponent> grid, Entity<MapGridComponent> map, Vector2i offset)
    {
        // Copy all tiles
        _tiles.Clear();

        var tiles = _map.GetAllTiles(grid.Owner, grid.Comp).ToList();
        foreach (var tileRef in tiles)
        {
            _tiles.Add((tileRef.GridIndices + offset, tileRef.Tile));
        }

        _map.SetTiles(map.Owner, map.Comp, _tiles);

        // Teleport all entities
        var ents = new HashSet<Entity<TransformComponent>>();
        _lookup.GetChildEntities(grid, ents);
        foreach (var (teleportEnt, xform) in ents)
        {
            var anchored = xform.Anchored;
            var newPos = new EntityCoordinates(map.Owner, xform.LocalPosition + offset);

            _transform.SetCoordinates(teleportEnt, newPos);
            if (anchored)
                _transform.AnchorEntity(teleportEnt);
        }

        // Spawn decals
        if (TryComp<DecalGridComponent>(grid.Owner, out var loadedDecals))
        {
            EnsureComp<DecalGridComponent>(map);

            var localBounds = grid.Comp.LocalAABB;
            foreach (var decal in _decals.GetDecalsIntersecting(grid.Owner, localBounds, loadedDecals))
            {
                var localPos = Transform(decal).Coordinates.Position;
                var targetPos = localPos + offset;

                var data = decal.Comp.Data;

                _decals.TryAddDecal(
                    data.Id,
                    new EntityCoordinates(map.Owner, targetPos),
                    out _,
                    data.Color,
                    data.Angle,
                    data.ZIndex,
                    data.Cleanable);
            }
        }

        Del(grid);
    }

    /// <summary>
    /// Returns and resolves all <see cref="LavalandGridRuinPrototype"/> given a dictionary of <see cref="ProtoId{LavalandGridRuinPrototype}"/>
    /// </summary>
    private List<LavalandGridRuinPrototype> GetGridRuinProtos(Dictionary<ProtoId<LavalandGridRuinPrototype>, ushort> protos)
    {
        var list = new List<LavalandGridRuinPrototype>();
        foreach (var (protoId, count) in protos)
        {
            if (!ProtoMan.Resolve(protoId, out var gridRuinPrototype))
                continue;

            for (var i = 0; i < count; i++)
            {
                list.Add(gridRuinPrototype);
            }
        }

        return list;
    }

    /// <summary>
    /// Returns and resolves all <see cref="LavalandDungeonRuinPrototype"/> given a dictionary of <see cref="ProtoId{LavalandDungeonRuinPrototype}"/>
    /// </summary>
    private List<LavalandDungeonRuinPrototype> GetDungeonRuinProtos(Dictionary<ProtoId<LavalandDungeonRuinPrototype>, ushort> protos)
    {
        var list = new List<LavalandDungeonRuinPrototype>();
        foreach (var (protoId, count) in protos)
        {
            if (!ProtoMan.Resolve(protoId, out var dungeonRuinPrototype))
                continue;

            for (var i = 0; i < count; i++)
            {
                list.Add(dungeonRuinPrototype);
            }
        }

        return list;
    }

    /// <summary>
    /// Gets the used space occupied by all the grids on the map, so we don't spawn ruins on top of it.
    /// </summary>
    /// <param name="mapId">The <see cref="MapId"/> of the map</param>
    /// <returns>A list of <see cref="Box2"/> that hold the used space</returns>
    private List<Box2> GetMapBoundary(MapId mapId)
    {
        var boundary = new List<Box2>();

        var grids = _map.GetAllGrids(mapId);
        foreach (var uid in grids)
        {
            var xform = Transform(uid);
            if (!_fixturesQuery.TryComp(uid, out var fixtures) || xform.MapUid is not { } mapUid)
                continue;

            var aabbs = new Box2();
            var transform = _physics.GetRelativePhysicsTransform((uid, xform), mapUid);
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard)
                    continue;

                var aabb = fixture.Shape.ComputeAABB(transform, 0);
                aabbs = aabbs.Union(aabb);
            }

            aabbs = aabbs.Enlarged(8f);
            boundary.Add(aabbs);
        }

        foreach (var box2 in boundary.ToList())
        {
            if (!box2.IsEmpty())
                continue;

            boundary.Remove(box2);
        }

        return boundary;
    }

    /// <summary>
    /// I don't fucking know what this does, and I'm not getting paid enough to analyze it
    /// </summary>
    private List<Vector2i> GetCoordinates(int distance, int maxDistance)
    {
        var coords = new List<Vector2i>();
        var moveVector = new Vector2i(maxDistance, maxDistance);
        while (moveVector.Y >= -maxDistance)
        {
            while (moveVector.X > -maxDistance)
            {
                coords.Add(moveVector);
                moveVector += new Vector2i(-distance, 0);
            }

            coords.Add(moveVector);
            moveVector += new Vector2i(0, -distance);

            while (moveVector.X < maxDistance)
            {
                coords.Add(moveVector);
                moveVector += new Vector2i(distance, 0);
            }

            coords.Add(moveVector);
            moveVector += new Vector2i(0, -distance);
        }

        return coords;
    }
}
