// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires.Haemomancer;

public sealed partial class ActionBloodBarrierSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionBloodBarrierComponent, BloodBarrierActionEvent>(OnAction);
    }

    private void OnAction(Entity<ActionBloodBarrierComponent> ent, ref BloodBarrierActionEvent args)
    {
        // If the target is a point, clear the hashset.
        if (args.Entity is { } targetedEntity && ent.Comp.Points.Contains(targetedEntity))
        {
            PredictedQueueDel(targetedEntity);
            ent.Comp.Points.Clear();
            Dirty(ent);
            return;
        }

        // Target must be a tile
        if (args.Entity is not null)
            return;

        var coords = args.Target;

        // We have one point, test if we are in distance to put another one
        if (ent.Comp.Points.Count == 1)
        {
            var pointACoords = Transform(ent.Comp.Points[0]).Coordinates;
            if (Vector2.Distance(pointACoords.Position, coords.Position) > ent.Comp.Distance)
            {
                // clear the previous point so user doesn't run away and forgets to clear it
                PredictedQueueDel(ent.Comp.Points[0]);
                ent.Comp.Points.Clear();
                Dirty(ent);
                return;
            }
        }

        var point = PredictedSpawnAtPosition(ent.Comp.PointProto, coords);
        ent.Comp.Points.Add(point);
        Dirty(ent);

        // We have gathered 2 points, start the barrier
        if (ent.Comp.Points.Count == 2)
        {
            var pointA = ent.Comp.Points[0];
            var pointB = ent.Comp.Points[1];
            SpawnBarrier(Transform(pointA),  Transform(pointB), ent.Comp.BarrierProto);

            // Clear both points
            foreach (var pointToDelete in ent.Comp.Points)
            {
                PredictedQueueDel(pointToDelete);
            }
            ent.Comp.Points.Clear();
            Dirty(ent);

            args.Handled = true;
        }
    }

    /// <summary>
    /// Spawns the barrier prototype between two points.
    /// </summary>
    private void SpawnBarrier(TransformComponent pointA, TransformComponent pointB, EntProtoId barrierProto)
    {
        var a = _transform.GetMapCoordinates(pointA);
        var b = _transform.GetMapCoordinates(pointB);
        if (a == b)
            return;

        var delta = b.Position - a.Position;
        var dirVec = delta.Normalized();
        var stopDist = delta.Length();
        var currentOffset = dirVec;

        while (currentOffset.Length() < stopDist)
        {
            var currentCoords = pointA.Coordinates.Offset(currentOffset);
            PredictedSpawnAtPosition(barrierProto, currentCoords);

            currentOffset += dirVec;
        }

        PredictedSpawnAtPosition(barrierProto, pointA.Coordinates);
        PredictedSpawnAtPosition(barrierProto, pointB.Coordinates);
    }
}
