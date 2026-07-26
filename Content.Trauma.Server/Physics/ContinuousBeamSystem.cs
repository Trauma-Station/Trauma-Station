// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Shared.Map;

namespace Content.Trauma.Server.Physics;

public sealed partial class ContinuousBeamSystem : SharedContinuousBeamSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private ComplexJointVisualsSystem _joint = default!;

    private readonly HashSet<Entity<DamageableComponent>> _targets = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ContinuousBeamGunComponent, ComplexJointVisualsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var gun, out var joint, out var xform))
        {
            if (gun.BeamTime > 0f && now > gun.BeamTimer)
            {
                StopFiring(uid, gun, joint);
                continue;
            }

            if (!UpdateBeamPosition(uid, gun, joint, xform, now))
                continue;

            var ev = new BeforeContinuousBeamDamageTickEvent((uid, gun, joint));
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
                continue;

            UpdateBeamDamage(uid, gun, joint, xform, now);
        }
    }

    private bool UpdateBeamPosition(EntityUid uid,
        ContinuousBeamGunComponent gun,
        ComplexJointVisualsComponent joint,
        TransformComponent xform,
        TimeSpan now)
    {
        if (now < gun.UpdateTimer)
            return true;

        gun.UpdateTimer = now + gun.UpdateTime;

        if (!ResolveBeamEndpointData(uid, gun, joint))
            return false;

        var target = gun.CursorPosition!.Value;
        var endpoint = gun.Endpoint!.Value;
        var endpointXform = Transform(endpoint);
        var pos = Xform.GetWorldPosition(endpointXform);
        var dir = target.Position - pos;
        var len = dir.Length();

        var ourPos = Xform.GetWorldPosition(xform);
        var newPos = pos + dir * gun.LaserSpeed / len;
        var dir2 = newPos - ourPos;
        var len2 = dir2.Length();

        if (len2 < 0.01f)
            return true;

        if (len <= gun.LaserSpeed)
            Xform.SetMapCoordinates((endpoint, endpointXform), target);
        else
        {
            var maxRange = MathF.Min(gun.MaxRangeOverride ?? gun.MinMaxLaserRange.Y, gun.MinMaxLaserRange.Y);
            var minRange = MathF.Min(gun.MinMaxLaserRange.X, maxRange);
            var newLen = Math.Clamp(len2, minRange, maxRange);

            Xform.SetMapCoordinates((endpoint, endpointXform),
                new MapCoordinates(ourPos + dir2 * newLen / len2, xform.MapID));
        }

        if (_joint.BeamCollision(uid, endpoint, gun.Data, false) is { } result && result.Count > 0)
        {
            var min = result.MinBy(x => x.Distance).HitPos;
            Xform.SetMapCoordinates((endpoint, endpointXform), new MapCoordinates(min, xform.MapID));
        }

        return true;
    }

    private void UpdateBeamDamage(EntityUid uid,
        ContinuousBeamGunComponent gun,
        ComplexJointVisualsComponent joint,
        TransformComponent xform,
        TimeSpan now)
    {
        if (now < gun.DamageTimer)
            return;

        gun.DamageTimer = now + gun.DamageTime;

        if (CalculateBeamDamageData((uid, gun, joint)) is not { } tuple)
            return;

        _targets.Clear();
        _lookup.GetEntitiesIntersecting(xform.MapID, tuple.boxRot, _targets, LookupFlags.Uncontained);
        foreach (var noob in _targets)
        {
            if (noob == gun.Shooter)
                continue;

            var beforeEv = new BeforeContinuousBeamDamagedEvent(uid, noob);
            RaiseLocalEvent(uid, ref beforeEv);

            if (beforeEv.Cancelled)
                continue;

            _dmg.TryChangeDamage(noob.Owner,
                gun.Damage * _body.GetVitalBodyPartRatio(noob.Owner),
                origin: uid,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll);

            if (gun.Effects is { } effects)
                _effects.ApplyEffects(noob, effects);

            if (TerminatingOrDeleted(noob.Owner))
                continue;

            var afterEv = new AfterContinuousBeamDamagedEvent(uid, noob);
            RaiseLocalEvent(uid, ref afterEv);
        }
    }
}
