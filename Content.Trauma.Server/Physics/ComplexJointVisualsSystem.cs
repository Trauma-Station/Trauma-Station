// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Physics;

public sealed partial class ComplexJointVisualsSystem : SharedComplexJointVisualsSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PhysicsSystem _physics = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(200);
    private TimeSpan _nextUpdate;

    private readonly Dictionary<string, HashSet<EntityUid>> _updatedIds = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;

        var query = EntityQueryEnumerator<ComplexJointVisualsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            _updatedIds.Clear();

            foreach (var (netEnt, data) in comp.Data)
            {
                if (!TryGetEntity(netEnt, out var target) || TerminatingOrDeleted(target.Value) ||
                    data.MaxRange is { } maxRange && !_transform.InRange(target.Value, uid, maxRange))
                {
                    comp.Data.Remove(netEnt);
                    continue;
                }

                if (_updatedIds.TryGetValue(data.Id, out var set))
                    set.Add(target.Value);
                else
                    _updatedIds[data.Id] = [target.Value];

                BeamCollision(uid, target.Value, data);
            }

            var ev = new ComplexJointUpdateEvent(uid, _updatedIds);
            RaiseLocalEvent(uid, ref ev);

            Dirty(uid, comp);
        }
    }

    public List<RayCastResults>? BeamCollision(EntityUid origin, EntityUid target, ComplexJointVisualsData data, bool raiseEvents = true)
    {
        if (!data.ShouldCollide)
            return null;

        var (originalOrigin, originalTarget) = (origin, target);

        if (data.ReverseBeam)
            (origin, target) = (target, origin);

        var originPos = _transform.GetMapCoordinates(origin);
        var targetPos = _transform.GetMapCoordinates(target);

        var dir = targetPos.Position - originPos.Position;

        var ray = new CollisionRay(originPos.Position, dir.Normalized(), (int) data.CollisionMask);
        var dist = dir.Length();
        if (data.MaxRange is { } maxRange)
            dist = Math.Min(dist, maxRange);

        var result = _physics.IntersectRay(originPos.MapId, ray, dist, origin, data.ReturnOnFirstHit).ToList();

        if (result.Count == 0 || !raiseEvents)
            return result;

        foreach (var hit in result)
        {
            if (data.CollisionIgnoreTarget && hit.HitEntity == target)
                continue;

            var ev = new ComplexJointCollisionEvent(originalOrigin, hit, originalTarget, data);
            RaiseLocalEvent(originalOrigin, ref ev);
        }

        return result;
    }
}
