// <Trauma>
using Content.Shared.Singularity.Components; // Goobstation - Radiation Overhaul
// </Trauma>
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.Radiation.Systems;

// main algorithm that fire radiation rays to target
public partial class RadiationSystem
{
    private readonly record struct SourceData(
        float Intensity,
        float Slope,
        float MaxRange,
        Entity<RadiationSourceComponent, TransformComponent> Entity,
        Vector2 WorldPosition)
    {
        public EntityUid Uid => Entity.Owner;
        public TransformComponent Transform => Entity.Comp2;
    }

    private void UpdateGridcast()
    {
        var debug = _debugSessions.Count > 0;
        var stopwatch = new Robust.Shared.Timing.Stopwatch();
        stopwatch.Start();

        var sourcesCount = _sourceDataMap.Count;
        if (_activeReceivers.Count == 0 || sourcesCount == 0)
        {
            RaiseLocalEvent(new RadiationSystemUpdatedEvent());
            return;
        }

        var results = new float[_activeReceivers.Count];
        var debugRays = debug ? new ConcurrentBag<DebugRadiationRay>() : null;

        var job = new RadiationJob
        {
            System = this,
            SourceTree = _sourceTree,
            SourceDataMap = _sourceDataMap,
            Destinations = _activeReceivers,
            Results = results,
            DebugRays = debugRays,
            Debug = debug
        };

        _parallel.ProcessNow(job, _activeReceivers.Count);

        for (var i = 0; i < _activeReceivers.Count; i++)
        {
            var uid = _activeReceivers[i];
            var rads = results[i];

            if (_receiverQuery.TryComp(uid, out var receiver))
            {
                receiver.CurrentRadiation = rads;
                if (rads > 0)
                    IrradiateEntity(uid, rads, GridcastUpdateRate);
            }
        }

        if (debugRays is not null)
            UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, sourcesCount, _activeReceivers.Count, debugRays.ToList());

        RaiseLocalEvent(new RadiationSystemUpdatedEvent());
    }

    private RadiationRay? Irradiate(SourceData source,
        EntityUid destUid,
        TransformComponent destTrs,
        Vector2 destWorld,
        bool saveVisitedTiles,
        List<Entity<MapGridComponent>> gridList)
    {
        var mapId = destTrs.MapID;

        // Goobstation Start - Radiation Overhaul
        // get direction from rad source to destination and its distance
        var dir = destWorld - source.WorldPosition;
        var dist = Math.Max(dir.Length(),0.5f);
        if (TryComp(source.Entity.Owner, out EventHorizonComponent? horizon)) // if we have a horizon emit radiation from the horizon,
            dist = Math.Max(dist - horizon.Radius, 0.5f);
        var rads = source.Intensity / (dist );
        if (rads < 0.01)
            return null;
        // Goobstation End - Radiation Overhaul

        var ray = new RadiationRay(mapId, source.Entity, source.WorldPosition, destUid, destWorld, rads);

        var box = Box2.FromTwoPoints(source.WorldPosition, destWorld);
        gridList.Clear();
        _mapManager.FindGridsIntersecting(mapId, box, ref gridList, true);

        foreach (var grid in gridList)
        {
            ray = Gridcast((grid.Owner, grid.Comp, Transform(grid)), ref ray, saveVisitedTiles, source.Transform, destTrs);
            if (ray.Rads <= 0)
                return ray;
        }

        return ray;
    }

    // Goobstation - Radiation Overhaul
    /// <summary>
    /// Similar to GridLineEnumerator, but also returns the distance the ray traveled in each cell
    /// </summary>
    /// <param name="sourceGridPos">source of the ray, in grid space</param>
    /// <param name="destGridPos"></param>
    /// <returns></returns>
    private static IEnumerable<(Vector2i cell, float distInCell)> AdvancedGridRaycast(Vector2 sourceGridPos, Vector2 destGridPos)
    {
        var delta = destGridPos - sourceGridPos;

        if (delta.LengthSquared() < 0.0001f)
        {
            yield return (new Vector2i((int)Math.Floor(sourceGridPos.X), (int)Math.Floor(sourceGridPos.Y)), 0f);
            yield break;
        }

        var currentX = (int)Math.Floor(sourceGridPos.X);
        var currentY = (int)Math.Floor(sourceGridPos.Y);
        var destX = (int)Math.Floor(destGridPos.X);
        var destY = (int)Math.Floor(destGridPos.Y);

        var stepX = 0;
        float tDeltaX = 0, tMaxX = float.MaxValue;
        if (delta.X != 0)
        {
            stepX = delta.X > 0 ? 1 : -1;
            float xEdge = stepX > 0 ? currentX + 1 : currentX;
            tMaxX = (xEdge - sourceGridPos.X) / delta.X;
            tDeltaX = stepX / delta.X;
        }

        var stepY = 0;
        float tDeltaY = 0, tMaxY = float.MaxValue;
        if (delta.Y != 0)
        {
            stepY = delta.Y > 0 ? 1 : -1;
            float yEdge = stepY > 0 ? currentY + 1 : currentY;
            tMaxY = (yEdge - sourceGridPos.Y) / delta.Y;
            tDeltaY = stepY / delta.Y;
        }

        var entry = sourceGridPos;
        var maxIterations = Math.Abs(destX - currentX) + Math.Abs(destY - currentY) + 2;
        var iterations = 0;

        while (true)
        {
            if (++iterations > maxIterations)
                yield break;

            var tExit = Math.Min(tMaxX, tMaxY);
            var exitIsX = tMaxX < tMaxY;
            if (tExit > 1f)
                tExit = 1f;
            var exit = sourceGridPos + delta * tExit;
            var cell = new Vector2i(currentX, currentY);
            yield return (cell, (exit - entry).Length());

            if (tExit >= 1f - 1e-6f)
                break;

            if (exitIsX)
            {
                currentX += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                currentY += stepY;
                tMaxY += tDeltaY;
            }
            entry = exit;
        }
    }
    private RadiationRay Gridcast(
        Entity<MapGridComponent, TransformComponent> grid,
        ref RadiationRay ray,
        bool saveVisitedTiles,
        TransformComponent sourceTrs,
        TransformComponent destTrs)
    {
        var blockers = saveVisitedTiles ? new List<(Vector2i, float)>() : null;
        var gridUid = grid.Owner;
        if (!_resistanceQuery.TryGetComponent(gridUid, out var resistance))
            return ray;

        var resistanceMap = resistance.ResistancePerTile;

        Vector2 srcLocal = sourceTrs.ParentUid == grid.Owner
            ? sourceTrs.LocalPosition
            : Vector2.Transform(ray.Source, grid.Comp2.InvLocalMatrix);

        Vector2 dstLocal = destTrs.ParentUid == grid.Owner
            ? destTrs.LocalPosition
            : Vector2.Transform(ray.Destination, grid.Comp2.InvLocalMatrix);

        // Goobstation Start - Radiation Overhaul
        Vector2 sourceGrid = new(
            srcLocal.X / grid.Comp1.TileSize,
            srcLocal.Y / grid.Comp1.TileSize);

        Vector2 destGrid = new(
            dstLocal.X / grid.Comp1.TileSize,
            dstLocal.Y / grid.Comp1.TileSize);

        foreach (var (point,dist) in AdvancedGridRaycast(sourceGrid,destGrid))
        {
            if (resistanceMap.TryGetValue(point, out var resData))
            {
                var passRatioFromRadResistance = (1 / (resData > 2 ? (resData / 2) : 1));

                var passthroughRatio = MathF.Pow(passRatioFromRadResistance, dist);
                ray.Rads *= passthroughRatio;

                // save data for debug
                if (saveVisitedTiles && blockers is not null)
                    blockers.Add((point, ray.Rads));

                // no intensity left after blocker
                if (ray.Rads <= MinIntensity)
                {
                    ray.Rads = 0;
                    break;
                }
            }
        }
        // Goobstation End - Radiation Overhaul

        if (blockers is null || blockers.Count == 0)
            return ray;

        ray.Blockers ??= new();
        ray.Blockers.Add(GetNetEntity(gridUid), blockers);
        return ray;
    }

    private float GetAdjustedRadiationIntensity(EntityUid uid, float rads)
    {
        var child = uid;
        var xform = Transform(uid);
        var parent = xform.ParentUid;

        while (parent.IsValid())
        {
            var parentXform = Transform(parent);
            var childMeta = MetaData(child);

            if ((childMeta.Flags & MetaDataFlags.InContainer) != MetaDataFlags.InContainer)
            {
                child = parent;
                parent = parentXform.ParentUid;
                continue;
            }

            if (_blockerQuery.TryComp(xform.ParentUid, out var blocker))
            {
                // Goobstation Start - Radiation Overhaul
                var ratio = blocker.RadDecay>2? 1 / (blocker.RadDecay/2):1;
                rads = (rads - blocker.RadResistance) * ratio;
                if (rads < 0.1)
                    return 0;
                // Goobstation End - Radiation Overhaul
            }

            child = parent;
            parent = parentXform.ParentUid;
        }

        return rads;
    }

    [UsedImplicitly]
    private readonly record struct RadiationJob : IParallelRobustJob
    {
        public int BatchSize => 5;
        public required RadiationSystem System { get; init; }
        public required B2DynamicTree<EntityUid> SourceTree { get; init; }
        public required Dictionary<EntityUid, SourceData> SourceDataMap { get; init; }
        public required List<EntityUid> Destinations { get; init; }
        public required float[] Results { get; init; }
        public required ConcurrentBag<DebugRadiationRay>? DebugRays { get; init; }
        public required bool Debug { get; init; }

        public void Execute(int index)
        {
            var destUid = Destinations[index];
            if (System.Deleted(destUid) || !System.TryComp(destUid, out TransformComponent? destTrs))
            {
                Results[index] = 0;
                return;
            }

            var nearbySourcesArray = ArrayPool<EntityUid>.Shared.Rent(256);

            var gridList = new List<Entity<MapGridComponent>>(8);

            try
            {
                var destWorld = System._transform.GetWorldPosition(destTrs);
                var rads = 0f;
                var destMapId = destTrs.MapID;

                var queryAabb = new Box2(destWorld, destWorld);

                var state = (nearbySourcesArray, 0, SourceTree);
                SourceTree.Query(ref state,
                    static (ref (EntityUid[] arr, int count, B2DynamicTree<EntityUid> tree) tuple,
                        DynamicTree.Proxy proxy) =>
                    {
                        if (tuple.count >= tuple.arr.Length)
                            return true;

                        var uid = tuple.tree.GetUserData(proxy);
                        tuple.arr[tuple.count++] = uid;
                        return true;
                    },
                    in queryAabb);

                var nearbySourcesSpan = nearbySourcesArray.AsSpan(0, state.Item2);

                foreach (var sourceUid in nearbySourcesSpan)
                {
                    if (!SourceDataMap.TryGetValue(sourceUid, out var source)
                        || source.Transform.MapID != destMapId)
                        continue;
                    var delta = source.WorldPosition - destWorld;
                    if (delta.LengthSquared() > source.MaxRange * source.MaxRange)
                        continue;
                    var dist = delta.Length();
                    var radsAfterDist = source.Intensity - source.Slope * dist;
                    if (radsAfterDist < System.MinIntensity)
                        continue;
                    if (System.Irradiate(source, destUid, destTrs, destWorld, Debug, gridList) is not { } ray)
                        continue;

                    if (ray.ReachedDestination)
                        rads += ray.Rads;

                    DebugRays?.Add(new DebugRadiationRay(
                        ray.MapId,
                        System.GetNetEntity(ray.SourceUid),
                        ray.Source,
                        System.GetNetEntity(ray.DestinationUid),
                        ray.Destination,
                        ray.Rads,
                        ray.Blockers ?? new Dictionary<NetEntity, List<(Vector2i, float)>>())
                    );
                }

                rads = System.GetAdjustedRadiationIntensity(destUid, rads);
                Results[index] = rads;
            }
            finally
            {
                ArrayPool<EntityUid>.Shared.Return(nearbySourcesArray);
            }
        }
    }
}
