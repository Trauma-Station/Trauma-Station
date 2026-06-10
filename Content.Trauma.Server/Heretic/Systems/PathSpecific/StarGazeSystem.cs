// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Physics;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class StarGazeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedStarMarkSystem _mark = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ThrowingSystem _throw = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;

    [Dependency] private EntityQuery<HereticMinionComponent> _minionQuery = default!;
    [Dependency] private EntityQuery<VocalComponent> _vocalQuery = default!;

    private readonly HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<StarGazeComponent, ComplexJointVisualsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var gaze, out var joint, out var xform))
        {
            if (!gaze.StartedBlasting)
                continue;

            if (!UpdateBeamState(uid, gaze, joint, now, out var stage))
                continue;

            if (!UpdateBeamPosition(uid, gaze, joint, xform, now))
                continue;

            UpdateBeamDamage(uid, gaze, joint, xform, now, stage);
        }
    }

    private void UpdateBeamDamage(EntityUid uid,
        StarGazeComponent gaze,
        ComplexJointVisualsComponent joint,
        TransformComponent xform,
        TimeSpan now,
        int stage)
    {
        if (now < gaze.DamageTimer || stage != 2)
            return;

        gaze.DamageTimer = now + gaze.DamageTime;

        if (!ResolveStarGazeEndpointData(uid, gaze, joint))
            return;

        var pos = _transform.GetWorldPosition(gaze.Endpoint!.Value);

        var gazerPos = _transform.GetWorldPosition(xform);

        var c = pos - gazerPos;
        var cLen = c.Length();

        if (cLen <= 0.01f)
            return;

        var cNorm = c / cLen;
        var angle = c.ToAngle();

        var offset = cNorm * gaze.BeamScale;
        var box = new Box2(gazerPos + offset + new Vector2(0f, -gaze.LaserThickness),
            gazerPos + offset + new Vector2(cLen, gaze.LaserThickness));
        var boxRot = new Box2Rotated(box, angle, gazerPos + offset);

        var minion = _minionQuery.CompOrNull(uid);

        _targets.Clear();
        _lookup.GetEntitiesIntersecting(xform.MapID, boxRot, _targets, LookupFlags.Dynamic);
        foreach (var noob in _targets)
        {
            if (noob == minion?.BoundHeretic)
                continue;

            if (_mobState.IsIncapacitated(noob, noob.Comp))
            {
                var coords = Transform(noob).Coordinates;
                _admin.Add(LogType.Gib,
                    LogImpact.Medium,
                    $"{ToPrettyString(uid):user} ashed {ToPrettyString(noob):target} using star gazer laser beam");
                /* Annoying popup spam
                _popup.PopupCoordinates(Loc.GetString("heretic-stargaze-obliterate-other",
                        ("uid", Identity.Entity(noob, EntityManager))),
                    coords,
                    Filter.PvsExcept(noob),
                    true,
                    PopupType.LargeCaution);*/
                _popup.PopupCoordinates(Loc.GetString("heretic-stargaze-obliterate-user"),
                    coords,
                    noob,
                    PopupType.LargeCaution);
                _audio.PlayPvs(gaze.ObliterateSound, coords);
                Spawn(gaze.AshProto, coords);
                QueueDel(noob); // Goodbye
                continue;
            }

            _mark.TryApplyStarMark(noob.AsNullable());
            _dmg.TryChangeDamage(noob.Owner,
                gaze.Damage * _body.GetVitalBodyPartRatio(noob.Owner),
                origin: uid,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll);

            if (_random.Prob(gaze.ScreamProb) && _vocalQuery.TryComp(noob, out var vocal))
                _chat.TryEmoteWithChat(noob, vocal.ScreamId);
        }

        var boxRot2 = new Box2Rotated(box.Enlarged(gaze.GravityPullSizeModifier), angle, gazerPos + offset);
        _targets.Clear();
        _lookup.GetEntitiesIntersecting(xform.MapID, boxRot2, _targets, LookupFlags.Dynamic);
        foreach (var noob in _targets)
        {
            if (noob == minion?.BoundHeretic)
                continue;

            var noobXform = Transform(noob);
            var noobPos = _transform.GetWorldPosition(noobXform);

            var a = pos + offset - noobPos;
            var b = gazerPos + offset - noobPos;
            var aLen = a.Length();
            var bLen = b.Length();

            if (aLen <= 0.01f || bLen <= 0.01f)
                continue;

            var angleac = MathF.Acos(Vector2.Dot(a / aLen, cNorm));
            var anglebc = MathF.Acos(Vector2.Dot(cNorm, b / -bLen));

            var sinac = MathF.Sin(angleac);
            var sinbc = MathF.Sin(anglebc);
            var anothersin = MathF.Sin(angleac + anglebc);
            var dist = cLen * sinac * sinbc / anothersin;

            var list = new List<(Vector2, float)>([(a / aLen, aLen), (b / bLen, bLen)]);

            var try1 = Angle.FromDegrees(90).RotateVec(cNorm);
            var try1Pos = noobPos + try1 * dist * 2f;
            var try2 = -try1;
            var try2Pos = noobPos + try2 * dist * 2f;

            if (DoIntersect(gazerPos + offset, pos + offset, noobPos, try1Pos))
                list.Add((try1, dist));
            else if (DoIntersect(gazerPos + offset, pos + offset, noobPos, try2Pos))
                list.Add((try2, dist));

            var result = list.MinBy(x => x.Item2);

            if (result.Item2 <= 0.01f)
                continue;

            var throwDir = result.Item1 * MathF.Min(gaze.MaxThrowLength, result.Item2);
            _throw.TryThrow(noob,
                throwDir,
                gaze.ThrowSpeed,
                recoil: false,
                animated: false,
                doSpin: false,
                playSound: false,
                predicted: false);
        }
    }

    private bool UpdateBeamPosition(EntityUid uid,
        StarGazeComponent gaze,
        ComplexJointVisualsComponent joint,
        TransformComponent xform,
        TimeSpan now)
    {
        if (now < gaze.UpdateTimer)
            return true;

        gaze.UpdateTimer = now + gaze.UpdateTime;

        if (!ResolveStarGazeEndpointData(uid, gaze, joint))
            return false;

        var target = gaze.CursorPosition!.Value;
        var endpoint = gaze.Endpoint!.Value;
        var endpointXform = Transform(endpoint);
        var pos = _transform.GetWorldPosition(endpointXform);
        var dir = target.Position - pos;
        var len = dir.Length();

        var gazerPos = _transform.GetWorldPosition(xform);
        var newPos = pos + dir * gaze.LaserSpeed / len;
        var dir2 = newPos - gazerPos;
        var len2 = dir2.Length();

        if (len2 < 0.01f)
            return true;

        if (len <= gaze.LaserSpeed)
            _transform.SetMapCoordinates((endpoint, endpointXform), target);
        else
        {
            var newLen = Math.Clamp(len2, gaze.MinMaxLaserRange.X, gaze.MinMaxLaserRange.Y);

            _transform.SetMapCoordinates((endpoint, endpointXform),
                new MapCoordinates(gazerPos + dir2 * newLen / len2, xform.MapID));
        }

        return true;
    }

    private bool UpdateBeamState(EntityUid uid,
        StarGazeComponent gaze,
        ComplexJointVisualsComponent joint,
        TimeSpan now,
        out int stage)
    {
        var difference = gaze.BeamTimer - now;

        if (difference < TimeSpan.Zero)
        {
            stage = 1;
            ClearJoints(uid, joint);
            QueueDel(gaze.Endpoint);
            RemCompDeferred(uid, gaze);
            return false;
        }

        stage = GetBeamStage((float) difference.TotalSeconds);

        if (stage == gaze.LastStage)
            return true;

        gaze.LastStage = stage;

        var jointData = GetJointData(joint);
        foreach (var data in jointData.Values)
        {
            if (data.Id != SharedStarGazerSystem.JointId)
                continue;

            var startSprite = gaze.Start2;
            var beamSprite = gaze.Beam2;
            var endSprite = gaze.End2;
            switch (stage)
            {
                case 1:
                    startSprite = gaze.Start1;
                    beamSprite = gaze.Beam1;
                    endSprite = gaze.End1;
                    break;
                case 3:
                    startSprite = gaze.Start3;
                    beamSprite = gaze.Beam3;
                    endSprite = gaze.End3;
                    break;
            }

            if (data.StartSprite == startSprite)
                continue;

            data.StartSprite = startSprite;
            data.Sprite = beamSprite;
            data.EndSprite = endSprite;
            Dirty(uid, joint);
        }

        return true;
    }

    private bool ResolveStarGazeEndpointData(EntityUid uid,
        StarGazeComponent gaze,
        ComplexJointVisualsComponent joint)
    {
        var exists = Exists(gaze.Endpoint);
        if (exists && gaze.CursorPosition != null)
            return true;

        ClearJoints(uid, joint);

        if (exists)
            QueueDel(gaze.Endpoint!.Value);

        RemCompDeferred(uid, gaze);
        return false;
    }

    private void ClearJoints(EntityUid uid,
        ComplexJointVisualsComponent joint,
        Dictionary<NetEntity, ComplexJointVisualsData>? jointData = null)
    {
        jointData ??= GetJointData(joint);

        if (joint.Data.Count >= jointData.Count)
            RemCompDeferred(uid, joint);
        else
        {
            joint.Data = joint.Data.ExceptBy(jointData.Keys, kvp => kvp.Key).ToDictionary();
            Dirty(uid, joint);
        }
    }

    public static int GetOrientation(Vector2 a, Vector2 b, Vector2 c)
    {
        var val = (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);

        if (val == 0)
            return 0;

        return val > 0 ? 1 : 2;
    }

    public static bool OnSegment(Vector2 a, Vector2 b, Vector2 c)
    {
        return b.X <= Math.Max(a.X, c.X) && b.X >= Math.Min(a.X, c.X) &&
               b.Y <= Math.Max(a.Y, c.Y) && b.Y >= Math.Min(a.Y, c.Y);
    }

    public static bool DoIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Find the four orientations needed for general and special cases
        var o1 = GetOrientation(p1, q1, p2);
        var o2 = GetOrientation(p1, q1, q2);
        var o3 = GetOrientation(p2, q2, p1);
        var o4 = GetOrientation(p2, q2, q1);

        // General case: segments intersect if orientations are different
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases (collinear points)
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1
        if (o1 == 0 && OnSegment(p1, p2, q1))
            return true;

        // p1, q1 and q2 are collinear and q2 lies on segment p1q1
        if (o2 == 0 && OnSegment(p1, q2, q1))
            return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2
        if (o3 == 0 && OnSegment(p2, p1, q2))
            return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2
        if (o4 == 0 && OnSegment(p2, q1, q2))
            return true;

        return false; // Doesn't fall in any of the above cases
    }

    private static int GetBeamStage(float time)
    {
        return time < 0.8f ? 1 : time > 9.7f ? 3 : 2;
    }

    private static Dictionary<NetEntity, ComplexJointVisualsData> GetJointData(ComplexJointVisualsComponent joint)
    {
        return joint.Data.Where(x => x.Value.Id == SharedStarGazerSystem.JointId).ToDictionary();
    }
}
