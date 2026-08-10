// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Server.CosmicCult.Components;
using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Coordinates;

namespace Content.Trauma.Server.CosmicCult.EntitySystems;

public sealed partial class CosmicCorruptingSystem : EntitySystem
{
    [Dependency] private MapSystem _map = default!;
    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private ITileDefinitionManager _tileMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TurfSystem _turfs = default!;
    [Dependency] private TransformSystem _transform = default!;

    private static readonly Vector2i[] NeighbourPositions =
    [
        new(-1, 1),
        new(0, 1),
        new(1, 1),
        new(-1, 0),
        new(0, 0),
        new(1, 0),
        new(-1, -1),
        new(0, -1),
        new(1, -1)
    ];

    private HashSet<Vector2i> _adding = new();

    //when the entity spawns, add all neighbouring tiles to the corruptable list
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CosmicCorruptingComponent> ent, ref MapInitEvent args)
    {
        RecalculateStartingTiles(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CosmicCorruptingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled || now < comp.CorruptionTimer)
                continue;

            comp.CorruptionTimer = _timing.CurTime + comp.CorruptionSpeed;
            ConvertTiles((uid, comp));
            if (comp.CorruptionTicks <= comp.CorruptionMaxTicks)
            {
                comp.CorruptionTicks++;
                comp.CorruptionChance -= comp.CorruptionReduction;
            }

            if (comp.CorruptionTicks >= comp.CorruptionMaxTicks)
                RemComp(uid, comp);
        }
    }

    private void ConvertTiles(Entity<CosmicCorruptingComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var convertTile = (ContentTileDefinition) _tileMan[ent.Comp.ConversionTile];

        //if this is a mobile corruptor, reset the list of corruptable tiles every attempt.
        //not a super clean solution because I didn't account for the astral nova in the first rewrite but it works well enough for our purposes.
        if (ent.Comp.Mobile)
            RecalculateStartingTiles(ent);

        //go over every corruptible tile
        _adding.Clear();
        ent.Comp.CorruptableTiles.RemoveWhere(pos =>
        {
            var tileRef = _map.GetTileRef((gridUid, mapGrid), pos);
            if (tileRef.Tile.TypeId == convertTile.TileId ||
                tileRef.Tile.IsEmpty) //if it's already corrupted (or space), remove it from the list and continue
            {
                return true;
            }

            if (!_rand.Prob(ent.Comp.CorruptionChance))
                return false;

            //replace & variantise the tile
            _tile.ReplaceTile(tileRef, convertTile);
            _tile.PickVariant(convertTile);

            //then add the new neighbours as targets as long as they're not already corrupted
            foreach (var neighbourPos in NeighbourPositions)
            {
                var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tileRef.GridIndices + neighbourPos);
                if (neighbourRef.Tile.TypeId == convertTile.TileId
                    || tileRef.Tile.IsEmpty) //ignore already corrupted (or space) tiles
                    continue;

                _adding.Add(neighbourRef.GridIndices);
            }

            //corrupt anything that can be corrupted
            foreach (var convertedEnt in _map.GetAnchoredEntities((gridUid, mapGrid), pos).ToList())
            {
                var proto = Prototype(convertedEnt);
                if (ent.Comp.EntityConversionDict.TryGetValue(proto?.ID!, out var conversion))
                {
                    ConvertEntity(convertedEnt, conversion);
                }
                else if (TryComp<CosmicCorruptibleComponent>(convertedEnt, out var corruptible)
                && corruptible.ConvertTo is { } result)
                {
                        ConvertEntity(convertedEnt, result);
                }
            }

            //spawn the vfx if we should
            if (ent.Comp.UseVFX)
                Spawn(ent.Comp.TileConvertVFX, _turfs.GetTileCenter(tileRef));

            return true;
        });
        ent.Comp.CorruptableTiles.UnionWith(_adding); // can't add them while iterating above
    }

    private void ConvertEntity(EntityUid convertedEnt, EntProtoId conversion)
    {
        var targetTransformComp = Transform(convertedEnt);
        var child = Spawn(conversion, _transform.GetMapCoordinates(convertedEnt, targetTransformComp));
        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);
        QueueDel(convertedEnt);
    }

    #region API

    public void SetCorruptionTime(Entity<CosmicCorruptingComponent> ent, TimeSpan time) =>
        ent.Comp.CorruptionSpeed = time;

    public void Enable(Entity<CosmicCorruptingComponent> ent, bool recalculate = true)
    {
        ent.Comp.Enabled = true;

        if (recalculate)
            RecalculateStartingTiles(ent);
    }

    public void RecalculateStartingTiles(Entity<CosmicCorruptingComponent> ent)
    {
        ent.Comp.CorruptableTiles.Clear();

        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var grid = (gridUid, mapGrid);
        var tile = _map.GetTileRef(grid, xform.Coordinates);

        if (ent.Comp.FloodFillStarting)
        {
            var convertTile = (ContentTileDefinition)_tileMan[ent.Comp.ConversionTile];
            var visitedTiles = new HashSet<Vector2i>();
            var tilesToVisit = new HashSet<Vector2i> { tile.GridIndices };

            var count = 0;

            while (tilesToVisit.Count > 0)
            {
                //get the first tile in the list
                var currtile = tilesToVisit.First();
                count++;

                //check every neighbouring tile
                foreach (var neighbourPos in NeighbourPositions)
                {
                    var neighbourRef = _map.GetTileRef((gridUid, mapGrid), currtile + neighbourPos);

                    //if it's already been converted
                    if (neighbourRef.Tile.TypeId == convertTile.TileId)
                    {
                        //and not already visited
                        if (!visitedTiles.Contains(neighbourRef.GridIndices))
                            tilesToVisit.Add(neighbourRef.GridIndices); //add it to the to visit list
                    }
                    else
                    {
                        //else, it's not been converted, so mark it as visited and add it to the corruptible tiles list
                        //we don't care if the tile is empty, that'll get checked later
                        visitedTiles.Add(neighbourRef.GridIndices);
                        ent.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
                    }
                }

                //finally, mark the tile as visited and remove it from the toVisit list
                visitedTiles.Add(currtile);
                tilesToVisit.Remove(currtile);
            }

            Log.Info($"floodfill tile recaulculation ran {count} times");
        }
        else
        {
            //add every neighbouring tile to the corruptable list
            //don't bother checking eligibility at this point because it'll get done later anyway
            foreach (var neighbourPos in NeighbourPositions)
            {
                var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tile.GridIndices + neighbourPos);

                ent.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
            }
        }
    }

    #endregion
}
