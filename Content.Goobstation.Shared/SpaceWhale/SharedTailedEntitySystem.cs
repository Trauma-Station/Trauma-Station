// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Weapons;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Physics.Events;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.SpaceWhale;

public abstract partial class SharedTailedEntitySystem : EntitySystem
{
    [Dependency] protected SharedTransformSystem TransformSystem = default!;

    [Dependency] protected EntityQuery<TailedEntitySegmentComponent> SegmentQuery = default!;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private EntityLookupSystem _look = default!;

    private readonly HashSet<Entity<TailedEntitySegmentComponent>> _lookSegments = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    [SubscribeLocalEvent]
    private void OnForceContract(Entity<TailedEntityComponent> ent, ref TailedEntityForceContractEvent args)
    {
        args.Handled = true;
        ent.Comp.PreventSegmentCollide = true;

        var coords = Transform(ent).Coordinates;
        var pos = GetNetCoordinates(coords);

        foreach (var data in ent.Comp.TailSegments)
        {
            if (!TryGetEntity(data.Segment, out var segment) || !SegmentQuery.TryComp(segment.Value, out var comp))
                continue;

            comp.Coords = pos;
            TransformSystem.SetCoordinates(segment.Value, coords);
            TransformSystem.AttachToGridOrMap(segment.Value);
            Dirty(segment.Value, comp);

            data.Coords = pos;
        }

        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<TailedEntityComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;

        var index = ent.Comp.TailSegments.FindIndex(x => TryGetEntity(x.Segment, out var e) && e == other);

        if (index < 0)
            return;

        if (ent.Comp.PreventSegmentCollide ||
            ent.Comp.PreventFirstSegmentsCollideAmount < 1 && index < ent.Comp.PreventFirstSegmentsCollideAmount)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttackAttempt(Entity<TailedEntityComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target is { } target &&
            ent.Comp.TailSegments.Any(x => TryGetEntity(x.Segment, out var e) && e == target))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnInRange(Entity<TailedEntityComponent> ent, ref MeleeInRangeEvent args)
    {
        if (args.Handled || !ent.Comp.MeleeAttackWithSegments)
            return;

        args.Handled = true;

        var segments = ent.Comp.TailSegments.Select(x => GetEntity(x.Segment)).ToList();
        segments.Insert(0, ent);

        foreach (var segment in segments)
        {
            if (!Exists(segment))
                continue;

            if (!CheckInRange(segment, segments, ref args))
                continue;

            args.User = segment;
            return;
        }
    }

    private bool CheckInRange(EntityUid ent, List<EntityUid> segments, ref MeleeInRangeEvent args)
    {
        args.InRange = args.TargetCoordinates is not { } targetCoords || args.TargetAngle is not { } angle
            ? _interaction.InRangeUnobstructed(ent, args.Target, args.Range, predicate: segments.Contains)
            : _interaction.InRangeUnobstructed(ent,
                args.Target,
                targetCoords,
                angle,
                args.Range,
                predicate: segments.Contains,
                overlapCheck: false);
        return args.InRange;
    }

    [SubscribeLocalEvent]
    private void OnGetRange(Entity<TailedEntityComponent> ent, ref GetLightAttackRangeEvent args)
    {
        if (!ent.Comp.MeleeAttackWithSegments || !TryComp(ent, out MeleeWeaponComponent? melee))
            return;

        args.Cancel = true;
        args.Range = ent.Comp.TailSegments.Count + melee.Range;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<TailedEntitySegmentComponent>();
        while (query.MoveNext(out var uid, out var segment))
        {
            ResetSegmentPosition((uid, segment));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<TailedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.TailSegments.Count == 0)
                continue;

            UpdateTailPositions((uid, comp, xform));
            UpdateCollision((uid, comp, xform));
        }
    }

    private void UpdateCollision(Entity<TailedEntityComponent, TransformComponent> ent)
    {
        if (!ent.Comp1.ShouldCollideWithSegments || !ent.Comp1.PreventSegmentCollide)
            return;

        _lookSegments.Clear();
        _look.GetEntitiesInRange(ent.Comp2.Coordinates, ent.Comp1.HeadRadius, _lookSegments, LookupFlags.Dynamic);
        if (ent.Comp1.TailSegments.Any(x =>
                TryGetEntity(x.Segment, out var e) && _lookSegments.Any(y => e == y.Owner)))
            return;

        ent.Comp1.PreventSegmentCollide = false;
        Dirty(ent, ent.Comp1);
    }

    protected void UpdateTailPositions(Entity<TailedEntityComponent, TransformComponent> ent)
    {
        if (_timing.ApplyingState)
            return;

        var (uid, comp, xform) = ent;

        var headPos = xform.Coordinates.Offset(comp.TailOffset);
        var lastPos = GetCoordinates(ent.Comp1.LastPos);

        if (headPos == lastPos)
            return;

        ent.Comp1.LastPos = GetNetCoordinates(headPos);

        Angle? headRot = null;
        for (var i = 0; i < comp.TailSegments.Count; i++)
        {
            var data = comp.TailSegments[i];

            var segPos = GetCoordinates(data.Coords);
            var nextPos = i <= 0 ? headPos : GetCoordinates(comp.TailSegments[i - 1].Coords) ?? headPos;

            // Compute the desired position: keep `Spacing` units behind the next entity along the line
            // from the segment to the next entity. If the segment is exactly on top of the target, fall back
            // to using the target's forward vector.
            var toTarget = segPos is not { } pos
                ? Vector2.Zero
                : nextPos.Position - TransformSystem.WithEntityId(pos, nextPos.EntityId).Position;
            var distance = toTarget.Length();

            var nextRot = Angle.FromWorldVec(toTarget);
            headRot ??= nextRot;

            EntityCoordinates desiredPos;
            if (distance > 0.0001f)
            {
                var dir = toTarget / distance;
                desiredPos = nextPos.Offset(-dir * comp.Spacing);
            }
            else
            {
                desiredPos = nextPos.Offset(-nextRot.ToWorldVec() * comp.Spacing);
            }

            var netDesired = GetNetCoordinates(desiredPos);

            comp.TailSegments[i].Coords = netDesired;

            if (!TryGetEntity(data.Segment, out var segment) ||
                !SegmentQuery.TryComp(segment.Value, out var segmentComp))
                continue;

            segmentComp.Coords = netDesired;
            segmentComp.WorldRotation = nextRot;

            Dirty(segment.Value, segmentComp);

            ResetSegmentPosition((segment.Value, segmentComp));
        }

        if (comp.HeadFollowSegmentRotation && headRot is { } rot)
            TransformSystem.SetWorldRotation(uid, rot);

        Dirty(uid, comp);
    }

    protected void ResetSegmentPosition(Entity<TailedEntitySegmentComponent> segment)
    {
        TransformSystem.SetWorldRotation(segment, segment.Comp.WorldRotation);
        if (segment.Comp.Coords is not { } coords)
            return;

        TransformSystem.SetCoordinates(segment, GetCoordinates(coords));
        TransformSystem.AttachToGridOrMap(segment);
    }
}
