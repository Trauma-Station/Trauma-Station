using System.Linq;
using Content.Trauma.Shared.AudioMuffle;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.AudioMuffle;

public sealed partial class AudioMuffleSystem
{
    [ViewVariables]
    public HashSet<MuffleTileData> UpdatedData = new();


    public static int ManhattanDistance(Vector2i start, Vector2i end)
    {
        var distance = end - start;
        return Math.Abs(distance.X) + Math.Abs(distance.Y);
    }

    private void RebuildAndExpand(Vector2i newPos, Vector2i oldPos)
    {
        if (newPos == oldPos)
            return;

        var difference = newPos - oldPos;
        var signX = MathF.Sign(difference.X);
        var signY = MathF.Sign(difference.Y);
        var distance = difference.X * signX + difference.Y * signY;

        if (distance >= AudioRange)
        {
            Log.Debug("Tree expansion: range exceeds limit - rebuilding tree");
            Expand(newPos, false);
            return;
        }

        var reverseDist = AudioRange - distance;

        if (!TileDataDict.TryGetValue(oldPos, out var oldData) || oldData.Previous != null ||
            !TileDataDict.TryGetValue(newPos, out var newData))
        {
            Log.Error("Tree expansion: node not found or is invalid - rebuilding tree");
            Expand(newPos, false);
            return;
        }

        var cur = newData;
        Vector2i? newPrev = null;
        for (var i = 0; i < AudioRange; i++)
        {
            cur.TotalCost = i;

            if (!TrySwapPrev(cur, newPrev, out var nextTile))
            {
                Log.Error("Tree expansion: reconstructing path failed - rebuilding tree");
                Expand(newPos, false);
                return;
            }

            if (nextTile == null || cur.Equals(oldData))
                break;

            newPrev = cur.Indices;

            if (!TileDataDict.TryGetValue(nextTile.Value, out cur))
            {
                Log.Error("Tree expansion: reconstructing path failed - rebuilding tree");
                Expand(newPos, false);
                return;
            }
        }

        if (!cur.Equals(oldData))
        {
            Log.Error("Tree expansion: reconstructing path failed - rebuilding tree");
            Expand(newPos, false);
            return;
        }

        newData.TotalCost = 0f;
        var reExpand = new HashSet<MuffleTileData>();
        if (!ExpandNode(newData, 0f, false, reExpand, out _))
        {
            Log.Debug("Tree expansion: node expansion failed - rebuilding tree");
            if (reExpand.Contains(newData))
            {
                Expand(newPos, false);
                return;
            }

            HashSet<Vector2i> invalidated = new();
            foreach (var node in reExpand)
            {
                if (invalidated.Contains(node.Indices))
                    continue;

                RewriteAndReExpand(node, invalidated, false);
            }
        }

        var frontier = new PriorityQueue<MuffleTileData>();
        HashSet<Vector2i> passed = new();
        foreach (var (tile, data) in TileDataDict)
        {
            bool isPassed = true;
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0 || x == 0 && y == 0)
                        continue;

                    var neighbor = tile + new Vector2i(x, y);

                    if (neighbor == data.Previous)
                        continue;

                    if (TileDataDict.ContainsKey(neighbor))
                        continue;

                    frontier.Add(data);
                    isPassed = false;
                }
            }

            if (isPassed)
                passed.Add(tile);
        }

        var vecX = new Vector2i(Math.Sign(signX - 1), Math.Sign(1 - signX)) * signX;
        var vecY = new Vector2i(Math.Sign(signY - 1), Math.Sign(1 - signY)) * signY;
        Expand(frontier, newPos, passed, vecX, vecY, false, AudioRange * distance);
    }

    private void Expand(Vector2i start, bool updateAudio = false)
    {
        var newNode = new MuffleTileData(start);

        TileDataDict.Clear();
        TileDataDict[start] = newNode;

        var vec = Vector2i.One;
        Expand(new PriorityQueue<MuffleTileData> { newNode }, start, new HashSet<Vector2i> { start }, vec, vec, updateAudio);
        if (updateAudio)
            ResetAudioOnPos(start);
    }

    private void Expand(PriorityQueue<MuffleTileData> frontier,
        Vector2i origin,
        HashSet<Vector2i> passed,
        Vector2i minMaxXDir,
        Vector2i minMaxYDir,
        bool updateAudio = false,
        int amount = AudioRange)
    {
        if (PlayerGrid is not { } grid || grid.Comp.Deleted)
            return;

        var minAbsX = Math.Abs(minMaxXDir.X);
        var maxAbsX = Math.Abs(minMaxXDir.Y);
        var minAbsY = Math.Abs(minMaxYDir.X);
        var maxAbsY = Math.Abs(minMaxYDir.Y);
        var sum = minAbsX + minAbsY + maxAbsX + maxAbsY;
        if (sum == 0)
            return;
        var max = MathF.Pow(amount, sum);
        var count = 0;

        HashSet<Vector2i> updated = new();

        while (frontier.Count > 0 && count < max)
        {
            var node = frontier.Take();
            count++;

            var cost = node.TotalCost;

            for (var x = -minAbsX; x <= maxAbsX; x++)
            {
                for (var y = -minAbsY; y <= maxAbsY; y++)
                {
                    if (x != 0 && y != 0 || x == 0 && y == 0)
                        continue;

                    var neighbor = node.Indices + new Vector2i(x, y);

                    if (neighbor == node.Previous)
                        continue;

                    if (passed.Contains(neighbor))
                        continue;

                    if (ManhattanDistance(origin, neighbor) > amount)
                        continue;

                    var moveCost = 1f;

                    var score = cost + moveCost + GetTotalTileCost(neighbor);

                    if (TileDataDict.TryGetValue(neighbor, out var next))
                    {
                        var diff = score - next.TotalCost;
                        if (diff >= 0)
                            continue;

                        next.Previous = node.Indices;
                        node.Next.Add(next);

                        UpdateTotalCostOfNextTileDatas(next, diff, false, AudioRange - count);

                        frontier.Add(next);
                    }
                    else
                    {
                        if (!_map.CollidesWithGrid(grid.Owner, grid.Comp, neighbor))
                            continue;

                        var newNode = new MuffleTileData(neighbor)
                        {
                            TotalCost = score,
                            Previous = node.Indices,
                        };

                        node.Next.Add(newNode);
                        TileDataDict[neighbor] = newNode;
                        frontier.Add(newNode);
                    }

                    updated.Add(neighbor);
                }
            }
        }

        if (updateAudio)
            ResetAllPosAudio(updated);
        CachedMuffleVolumeDict.Clear();
    }

    private void RewriteAndReExpand(MuffleTileData first,
        HashSet<Vector2i> invalidated,
        bool updateAudio = false,
        int amount = AudioRange)
    {
        if (first.Previous == null)
        {
            Expand(first.Indices, updateAudio);
            return;
        }

        PriorityQueue<MuffleTileData> queue = new() { first };
        HashSet<Vector2i> updated = new() { first.Indices };

        InvalidateNext(first, invalidated);
        invalidated.Remove(first.Indices);
        first.Next.Clear();

        foreach (var node in TileDataDict.Values)
        {
            if (ShouldAddToQueue(node, invalidated))
                queue.Add(node);
        }

        var count = 0;
        while (queue.Count > 0 && count < Math.Pow(amount, 4))
        {
            var node = queue.Take();
            count++;

            if (invalidated.Contains(node.Indices))
                continue;

            var cost = node.TotalCost;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0 || x == 0 && y == 0)
                        continue;

                    var neighbor = node.Indices + new Vector2i(x, y);

                    if (neighbor == node.Previous)
                        continue;

                    if (!invalidated.Contains(neighbor))
                        continue;

                    if (ManhattanDistance(node.Indices, neighbor) > amount)
                        continue;

                    invalidated.Remove(neighbor);

                    var moveCost = 1f;

                    var score = cost + moveCost + GetTotalTileCost(neighbor);

                    var newNode = new MuffleTileData(neighbor)
                    {
                        TotalCost = score,
                        Previous = node.Indices,
                    };

                    node.Next.Add(newNode);
                    TileDataDict[neighbor] = newNode;
                    queue.Add(newNode);
                    updated.Add(neighbor);
                }
            }
        }

        if (updateAudio)
            ResetAllPosAudio(updated);
    }

    private bool ShouldAddToQueue(MuffleTileData node, HashSet<Vector2i> invalidated)
    {
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x != 0 && y != 0 || x == 0 && y == 0)
                    continue;

                var neighbor = node.Indices + new Vector2i(x, y);

                if (invalidated.Contains(neighbor))
                    return true;
            }
        }

        return false;
    }

    private void InvalidateNext(MuffleTileData node, HashSet<Vector2i> invalidatedIndices, bool invalidateSelf = false)
    {
        if (invalidatedIndices.Contains(node.Indices))
            return;

        if (invalidateSelf)
            TileDataDict.Remove(node.Indices);

        invalidatedIndices.Add(node.Indices);

        foreach (var next in node.Next)
        {
            InvalidateNext(next, invalidatedIndices, true);
        }
    }

    private bool ExpandNode(MuffleTileData node,
        float delta,
        bool resetAudio,
        HashSet<MuffleTileData> nodesToReExpand,
        out HashSet<MuffleTileData> nextNodesToExpand,
        bool firstIteration = true,
        int iteration = AudioRange)
    {
        nextNodesToExpand = new();

        if (iteration <= 0)
            return true;

        if (nodesToReExpand.Contains(node))
            return true;

        if (firstIteration)
            UpdatedData.Clear();
        else if (UpdatedData.Contains(node))
            return true;

        UpdatedData.Add(node);

        node.TotalCost += delta;
        var expansionNodes = GetExpansionNodes(node);
        var result = true;
        foreach (var (next, score) in expansionNodes)
        {
            if (node.Previous == next.Indices)
                continue;

            nextNodesToExpand.Add(next);

            var diff = score - next.TotalCost;

            if (next.Previous != node.Indices && diff <= 0 || node.Previous == null)
            {
                if (TrySwapPrev(next, node.Indices, out _))
                    next.TotalCost = score;

                nodesToReExpand.Add(node);
                result = false;
            }
            else if (next.Previous == node.Indices && diff > 0 && !ConnectToNewPreviousNode(next))
            {
                nodesToReExpand.Add(node);
                result = false;
            }
        }

        if (!result)
            return false;

        var toUpdate = delta < 0 ? node.TotalCost : node.TotalCost - delta;
        if (iteration <= 1)
        {
            if (firstIteration)
                UpdateTotalCostOfNextTileDatas(node, toUpdate, resetAudio, iteration, true);
            return true;
        }

        PriorityQueue<MuffleTileData> frontier = new();
        foreach (var next in nextNodesToExpand)
        {
            if (!UpdatedData.Contains(next))
                frontier.Add(next);
        }

        var count = 0;
        while (frontier.Count > 0 && count < Math.Pow(AudioRange, 4))
        {
            var next = frontier.Take();
            count++;

            if (!ExpandNode(next, 0f, resetAudio, nodesToReExpand, out var nodes, false, 1))
                result = false;

            foreach (var toAdd in nodes)
            {
                if (UpdatedData.Contains(toAdd))
                    continue;

                frontier.Add(toAdd);
            }
        }

        UpdateTotalCostOfNextTileDatas(node, toUpdate, resetAudio, iteration, true);

        return result;
    }

    private bool ConnectToNewPreviousNode(MuffleTileData node)
    {
        var list = GetExpansionNodes(node).Keys.ToList();
        if (list.Count == 0)
            return true;

        list.Sort();
        var result = list[^1];
        if (node.Previous == result.Indices)
            return true;

        var prev = result.Previous;

        if (prev == null || prev != node.Indices)
        {
            if (!TrySwapPrev(node, result.Indices, out _))
                return false;

            node.TotalCost = result.TotalCost + 1f + GetTotalTileCost(node.Indices);
        }
        else
            return false; // It's easier to rebuild this shit than trying to figure this out...

        return true;
    }

    private Dictionary<MuffleTileData, float> GetExpansionNodes(MuffleTileData node)
    {
        var cost = node.TotalCost;
        Dictionary<MuffleTileData, float> exoansionNodes = new();
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x != 0 && y != 0 || x == 0 && y == 0)
                    continue;

                var neighbor = node.Indices + new Vector2i(x, y);

                if (node.Previous == neighbor)
                    continue;

                if (!TileDataDict.TryGetValue(neighbor, out var next))
                    continue;

                var moveCost = 1f;

                var score = moveCost + GetTotalTileCost(neighbor) + cost;

                exoansionNodes[next] = score;
            }
        }

        return exoansionNodes;
    }

    private bool TrySwapPrev(MuffleTileData data, Vector2i? newPrevTile, out Vector2i? nextPrevious)
    {
        nextPrevious = data.Previous;

        if (nextPrevious != null && nextPrevious == newPrevTile)
            return true;

        data.Previous = newPrevTile;

        if (nextPrevious != null && TileDataDict.TryGetValue(nextPrevious.Value, out var nextPrev))
            nextPrev.Next.Remove(data);

        if (newPrevTile == null)
            return true;

        if (!TileDataDict.TryGetValue(newPrevTile.Value, out var newPrev))
            return false;

        data.Next.Remove(newPrev);
        newPrev.Next.Add(data);

        return true;
    }

    private void UpdateTotalCostOfNextTileDatas(MuffleTileData data,
        float delta,
        bool resetAudio,
        int iteration = AudioRange,
        bool reCalculate = false,
        bool firstIteration = true)
    {
        if (firstIteration)
            UpdatedData.Clear();
        else if (UpdatedData.Contains(data))
            return;

        var cost = reCalculate ? GetTotalTileCost(data.Indices) : data.TotalCost;
        var newCost = MathF.Max(0f, cost + delta);

        data.TotalCost = newCost;

        if (resetAudio)
            ResetAudioOnPos(data.Indices);

        UpdatedData.Add(data);

        if (iteration <= 0)
            return;

        var nextDelta = reCalculate ? newCost + 1f : delta;
        foreach (var next in data.Next)
        {
            UpdateTotalCostOfNextTileDatas(next, nextDelta, resetAudio, iteration - 1, reCalculate, false);
        }
    }

    private void AddOrRemoveBlocker(Entity<SoundBlockerComponent?> blocker,
        Vector2i indices,
        bool add,
        bool modifyCost,
        bool resetAudio = false)
    {
        if (!TryGetBlockerCost(blocker, out var cost))
            return;

        if (add)
        {
            BlockerIndicesDict[blocker] = indices;
            ReverseBlockerIndicesDict.GetOrNew(indices).Add(blocker);
        }
        else if (ReverseBlockerIndicesDict.TryGetValue(indices, out var blockers))
        {
            blockers.Remove(blocker);
            if (blockers.Count == 0)
                ReverseBlockerIndicesDict.Remove(indices);
        }

        if (!modifyCost)
            return;

        var sign = add ? 1 : -1;

        ModifyBlockerAmount(indices, sign * cost, resetAudio);
    }

    private void ModifyBlockerAmount(Vector2i indices, float delta, bool resetAudio = false)
    {
        if (!TileDataDict.TryGetValue(indices, out var data))
            return;

        if (delta < 0 && delta < -data.TotalCost)
            delta = -data.TotalCost;

        var reExpand = new HashSet<MuffleTileData>();
        if (!ExpandNode(data, delta, resetAudio, reExpand, out _, true, 1))
        {
            Log.Debug("Failed to gracefully rebuild the tree upon adding/removing blocker");
            HashSet<Vector2i> invalidated = new();
            foreach (var node in reExpand)
            {
                if (invalidated.Contains(node.Indices))
                    continue;

                RewriteAndReExpand(node, invalidated);
            }
        }
    }

    public sealed class MuffleTileData(Vector2i indices) : IEquatable<MuffleTileData>, IComparable<MuffleTileData>
    {
        public readonly Vector2i Indices = indices;

        public float TotalCost;

        // TODO: make this into MuffleTileData
        public Vector2i? Previous;

        public HashSet<MuffleTileData> Next = new(4);

        public bool Equals(MuffleTileData? other)
        {
            return other != null && Indices.Equals(other.Indices);
        }

        public int CompareTo(MuffleTileData? other)
        {
            return other == null ? 1 : other.TotalCost.CompareTo(TotalCost);
        }

        public override int GetHashCode()
        {
            return Indices.GetHashCode();
        }
    }
}
