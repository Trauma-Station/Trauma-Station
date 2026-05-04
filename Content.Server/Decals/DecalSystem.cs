using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Decals;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Server.Decals;

// Trauma - completely rewrote decals to be entity based
public sealed class DecalSystem : SharedDecalSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private static readonly Vector2 _boundsMinExpansion = new(0.01f, 0.01f);
    private static readonly Vector2 _boundsMaxExpansion = new(1.01f, 1.01f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestDecalPlacementEvent>(OnDecalPlacementRequest);
        SubscribeNetworkEvent<RequestDecalRemovalEvent>(OnDecalRemovalRequest);
        SubscribeLocalEvent<PostGridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref PostGridSplitEvent ev)
    {
        if (!GridQuery.TryComp(ev.OldGrid, out DecalGridComponent? oldComp))
            return;

        if (!GridQuery.TryComp(ev.Grid, out DecalGridComponent? newComp))
            return;

        // Transfer decals over to the new grid.
        var enumerator = Map.GetAllTilesEnumerator(ev.Grid, MapGridQuery.Comp(ev.Grid));

        var oldChunkCollection = oldComp.ChunkCollection.ChunkCollection;
        var chunkCollection = newComp.ChunkCollection.ChunkCollection;

        while (enumerator.MoveNext(out var tile))
        {
            var tilePos = (Vector2) tile.Value.GridIndices;
            var chunkIndices = GetChunkIndices(tilePos);

            if (!oldChunkCollection.TryGetValue(chunkIndices, out var oldChunk))
                continue;

            var bounds = new Box2(tilePos - _boundsMinExpansion, tilePos + _boundsMaxExpansion);

            oldChunk.Decals.RemoveWhere(data =>
            {
                if (!bounds.Contains(data.Coordinates))
                    return false;

                var decal = data.Ent;
                var newChunk = chunkCollection.GetOrNew(chunkIndices);
                newChunk.Decals.Add(data);
                if (decal.Comp.Chunk != chunkIndices)
                {
                    decal.Comp.Chunk = chunkIndices;
                    DirtyField(decal, decal.Comp, nameof(DecalComponent.Chunk));
                }
                return true;
            });
        }
    }

    private void OnDecalPlacementRequest(RequestDecalPlacementEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        // bad
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        if (!TryAddDecal(ev.Decal, coordinates, out _))
            return;

        if (eventArgs.SenderSession.AttachedEntity != null)
        {
            _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                $"{ToPrettyString(eventArgs.SenderSession.AttachedEntity.Value):actor} drew a {ev.Decal.Color} {ev.Decal.Id} at {ev.Coordinates}");
        }
        else
        {
            _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                $"{eventArgs.SenderSession.Name} drew a {ev.Decal.Color} {ev.Decal.Id} at {ev.Coordinates}");
        }
    }

    private void OnDecalRemovalRequest(RequestDecalRemovalEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        // bad
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        var gridId = Xform.GetGrid(coordinates);

        if (gridId == null)
            return;

        // remove all decals on the same tile
        foreach (var decal in GetDecalsInRange(gridId.Value, ev.Coordinates.Position))
        {
            var data = decal.Comp.Data;
            if (eventArgs.SenderSession.AttachedEntity != null)
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{ToPrettyString(eventArgs.SenderSession.AttachedEntity.Value):actor} removed a {data.Color} {data.Id} at {ev.Coordinates}");
            }
            else
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{eventArgs.SenderSession.Name} removed a {data.Color} {data.Id} at {ev.Coordinates}");
            }

            Del(decal);
        }
    }
}
