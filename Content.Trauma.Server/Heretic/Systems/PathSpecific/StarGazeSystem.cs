// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Server.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class StarGazeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedStarMarkSystem _mark = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ThrowingSystem _throw = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedComplexJointVisualsSystem _joint = default!;
    [Dependency] private SharedContinuousBeamSystem _beam = default!;

    [Dependency] private EntityQuery<HereticMinionComponent> _minionQuery = default!;
    [Dependency] private EntityQuery<VocalComponent> _vocalQuery = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;
    [Dependency] private EntityQuery<GhoulComponent> _ghoulQuery = default!;

    private readonly HashSet<Entity<PhysicsComponent>> _pullTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StarGazeComponent, BeforeContinuousBeamDamagedEvent>(OnBeforeDamage, after: [typeof(GhoulSystem)]);
    }

    private void OnBeforeDamage(Entity<StarGazeComponent> ent, ref BeforeContinuousBeamDamagedEvent args)
    {
        if (args.Cancelled || !_mobState.IsIncapacitated(args.Target))
            return;

        var coords = Transform(args.Target).Coordinates;
        _admin.Add(LogType.Gib, LogImpact.Medium, $"{ent.Owner} ashed {args.Target} using star gazer laser beam");
        _popup.PopupCoordinates(Loc.GetString("heretic-stargaze-obliterate-user"),
            coords,
            args.Target,
            PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.ObliterateSound, coords);
        Spawn(ent.Comp.AshProto, coords);
        QueueDel(args.Target); // Goodbye
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void AfterDamage(Entity<StarGazeComponent> ent, ref AfterContinuousBeamDamagedEvent args)
    {
        _mark.TryApplyStarMark(args.Target);
        if (_random.Prob(ent.Comp.ScreamProb) && _vocalQuery.TryComp(args.Target, out var vocal))
            _chat.TryEmoteWithChat(args.Target, vocal.ScreamId);
    }

    [SubscribeLocalEvent]
    private void OnStopFiring(Entity<StarGazeComponent> ent, ref ContinuousBeamStoppedFiringEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamageTick(Entity<StarGazeComponent> ent, ref BeforeContinuousBeamDamageTickEvent args)
    {
        Entity<StarGazeComponent, ContinuousBeamGunComponent, ComplexJointVisualsComponent> combined = (ent, ent,
            args.Ent, args.Ent);

        if (!UpdateBeamState(combined))
            args.Cancelled = true;
        else
            PullVictims(combined);
    }

    private bool UpdateBeamState(Entity<StarGazeComponent, ContinuousBeamGunComponent, ComplexJointVisualsComponent> ent)
    {
        var difference = ent.Comp2.BeamTimer - _timing.CurTime;

        var stage = GetBeamStage((float) difference.TotalSeconds);

        if (stage == ent.Comp1.LastStage)
            return stage == 2;

        ent.Comp1.LastStage = stage;

        var jointData = _joint.GetJointData(ent.Comp3, SharedStarGazerSystem.JointId);
        foreach (var data in jointData.Values)
        {
            if (data.Id != SharedStarGazerSystem.JointId)
                continue;

            var startSprite = ent.Comp1.Start2;
            var beamSprite = ent.Comp1.Beam2;
            var endSprite = ent.Comp1.End2;
            switch (stage)
            {
                case 1:
                    startSprite = ent.Comp1.Start1;
                    beamSprite = ent.Comp1.Beam1;
                    endSprite = ent.Comp1.End1;
                    break;
                case 3:
                    startSprite = ent.Comp1.Start3;
                    beamSprite = ent.Comp1.Beam3;
                    endSprite = ent.Comp1.End3;
                    break;
            }

            if (data.StartSprite == startSprite)
                continue;

            data.StartSprite = startSprite;
            data.Sprite = beamSprite;
            data.EndSprite = endSprite;
            Dirty(ent, ent.Comp2);
        }

        return stage == 2;
    }

    private void PullVictims(Entity<StarGazeComponent, ContinuousBeamGunComponent, ComplexJointVisualsComponent> ent)
    {
        if (_beam.CalculateBeamDamageData((ent, ent, ent)) is not { } tuple)
            return;

        var (boxRot1, angle, cLen, cNorm, offset, gazerPos, pos) = tuple;
        var box = boxRot1.Box;

        var heretic = _minionQuery.CompOrNull(ent)?.BoundHeretic;

        var boxRot2 = new Box2Rotated(box.Enlarged(ent.Comp1.GravityPullSizeModifier), angle, gazerPos + offset);
        _pullTargets.Clear();
        _lookup.GetEntitiesIntersecting(Transform(ent).MapID, boxRot2, _pullTargets, LookupFlags.Dynamic);
        foreach (var noob in _pullTargets)
        {
            if (noob == ent.Comp2.Shooter || noob == heretic || _ghostQuery.HasComp(noob) || _ghoulQuery.HasComp(noob))
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

            var throwDir = result.Item1 * MathF.Min(ent.Comp1.MaxThrowLength, result.Item2);
            _throw.TryThrow(noob,
                throwDir,
                ent.Comp1.ThrowSpeed,
                recoil: false,
                animated: false,
                doSpin: false,
                playSound: false,
                predicted: false,
                throwInAir: false);
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
}
