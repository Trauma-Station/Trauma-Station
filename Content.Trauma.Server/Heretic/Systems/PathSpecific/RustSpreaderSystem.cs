// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Content.Trauma.Server.Heretic.Abilities;
using Content.Trauma.Server.Heretic.Components.PathSpecific;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class RustSpreaderSystem : EntitySystem
{
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private HereticAbilitySystem _ability = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private static readonly TimeSpan RustSpreadInterval = TimeSpan.FromSeconds(2);
    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RustSpreaderComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<RustSpreaderComponent, HereticStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(Entity<RustSpreaderComponent> ent, ref HereticStateChangedEvent args)
    {
        ent.Comp.Paused = args.IsDead;
    }

    private void OnInit(Entity<RustSpreaderComponent> ent, ref MapInitEvent args)
    {
        var coords = _xform.GetMapCoordinates(ent.Owner);
        if (!_mapManager.TryFindGridAt(coords, out var gridUid, out var grid))
        {
            QueueDel(ent);
            return;
        }

        var tile = _map.GetTileRef((gridUid, grid), coords);
        ent.Comp.TilesToRust.Enqueue(tile);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (_nextUpdate > now)
            return;

        _nextUpdate = now + RustSpreadInterval;

        var gridQuery = GetEntityQuery<MapGridComponent>();
        var dockQuery = GetEntityQuery<DockingComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var query = EntityQueryEnumerator<RustSpreaderComponent>();
        while (query.MoveNext(out var spreader))
        {
            if (spreader.Paused)
                continue;

            List<EntityUid> toRemove = new();
            foreach (var ent in spreader.AffectedDocks)
            {
                if (!Exists(ent) || !dockQuery.TryGetComponent(ent, out var dock))
                {
                    toRemove.Add(ent);
                    continue;
                }

                if (!dock.Docked ||
                    !xformQuery.TryGetComponent(dock.DockedWith, out var dockedXform) ||
                    !gridQuery.TryComp(dockedXform.GridUid, out var dockedGrid))
                    continue;

                var neighborPos = _map.CoordinatesToTile(dockedXform.GridUid.Value,
                    dockedGrid,
                    dockedXform.Coordinates);

                var neighbor = _map.GetTileRef(dockedXform.GridUid.Value, dockedGrid, neighborPos);
                if (neighbor.Tile.IsEmpty || spreader.ProcessedTiles.Contains(neighbor))
                    continue;

                spreader.TilesToRust.Enqueue(neighbor);
                spreader.ProcessedTiles.Add(neighbor);
            }

            foreach (var remove in toRemove)
            {
                spreader.AffectedDocks.Remove(remove);
            }

            var count = spreader.AmountToRust;
            for (var i = 0; i < count; i++)
            {
                if (spreader.TilesToRust.Count == 0)
                    break;

                var tile = spreader.TilesToRust.Dequeue();

                if (!gridQuery.TryComp(tile.GridUid, out var mapGrid))
                    continue;

                // In case tile has changed
                tile = _map.GetTileRef(tile.GridUid, mapGrid, tile.GridIndices);
                spreader.ProcessedTiles.Add(tile);

                var ourEnts = _map.GetAnchoredEntitiesEnumerator(tile.GridUid, mapGrid, tile.GridIndices);
                List<EntityUid> toRust = new();
                while (ourEnts.MoveNext(out var ent))
                {
                    if (dockQuery.HasComp(ent.Value))
                        spreader.AffectedDocks.Add(ent.Value);
                    else
                        toRust.Add(ent.Value);
                }

                foreach (var ent in toRust)
                {
                    _ability.TryMakeRustWall(ent);
                }

                for (var j = 0; j < 4; j++)
                {
                    var atmosDir = (AtmosDirection) (1 << j);
                    var neighborPos = tile.GridIndices.Offset(atmosDir);
                    var neighbor = _map.GetTileRef(tile.GridUid, mapGrid, neighborPos);

                    if (neighbor.Tile.IsEmpty || spreader.ProcessedTiles.Contains(neighbor))
                        continue;

                    spreader.TilesToRust.Enqueue(neighbor);
                    spreader.ProcessedTiles.Add(neighbor);
                }

                var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

                if (_ability.CanRustTile(tileDef))
                    _ability.MakeRustTile(tile.GridUid, mapGrid, tile, spreader.TileRune);
            }

            if (spreader.TilesToRust.Count > 0)
                spreader.AmountToRust += 4;
        }
    }
}
