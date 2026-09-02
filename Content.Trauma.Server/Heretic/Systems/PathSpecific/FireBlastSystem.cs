// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Religion;
using Content.Medical.Common.Targeting;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Atmos.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Server.Physics;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;
using Content.Trauma.Shared.Heretic.Systems;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class FireBlastSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStaminaSystem _stam = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private ComplexJointVisualsSystem _joint = default!;
    [Dependency] private EntityQuery<FlammableComponent> _flammableQuery = default!;
    [Dependency] private EntityQuery<GhoulComponent> _ghoulQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;
    [Dependency] private EntityQuery<FireBlastedComponent> _fireBlastQuery = default!;

    private HashSet<Entity<MobStateComponent>> _targets = new();

    private static readonly EntProtoId FireBlastStatusEffect = "StatusEffectFireBlasted";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<FireBlastedComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var comp, out var dmg))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;

            if (comp.Damage == 0f)
                continue;

            var damage = new DamageSpecifier
            {
                DamageDict =
                {
                    { "Heat", comp.Damage },
                },
            };

            _dmg.ChangeDamage((uid, dmg), damage, interruptsDoAfters: false, targetPart: TargetBodyPart.Vital);

            var stamDmg = comp.Damage * comp.StaminaDamageMultiplier;
            _stam.TakeOvertimeStaminaDamage(uid, stamDmg);
        }
    }

    [SubscribeLocalEvent]
    private void UpdateBeams(Entity<FireBlastedComponent> ent, ref ComplexJointUpdateEvent args)
    {
        if (args.UpdatedIds.ContainsKey(ent.Comp.FireBlastBeamDataId))
            return;

        ent.Comp.ShouldBounce = false;
        _status.TryRemoveStatusEffect(ent, FireBlastStatusEffect);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<FireBlastedComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _joint.ClearBeamJoints(ent.Owner, ent.Comp.FireBlastBeamDataId);

        if (!ent.Comp.ShouldBounce || TrySendBeam(ent) || ent.Comp.HitEntities.Count < ent.Comp.BouncesForBonusEffect)
            return;

        BonusEffect(ent);
    }

    private void BonusEffect(Entity<FireBlastedComponent> origin)
    {
        var pos = Transform(origin).Coordinates;

        Spawn(origin.Comp.BonusEffect, pos);
        _audio.PlayPvs(origin.Comp.Sound, pos);

        GetTargets(Transform(origin), origin.Comp.BonusRange);
        foreach (var ent in _targets)
        {
            var uid = ent.Owner;

            _flammable.AdjustFireStacks(uid,
                origin.Comp.BonusFireStacks,
                null,
                true,
                origin.Comp.FireProtectionPenetration);

            _stun.KnockdownOrStun(uid, origin.Comp.BonusKnockdownTime);
            _dmg.ChangeDamage(uid, origin.Comp.FireBlastBonusDamage, targetPart: TargetBodyPart.Vital);
        }
    }

    private void GetTargets(TransformComponent xform, float range)
    {
        _targets.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, range, _targets, flags: LookupFlags.Dynamic);
        _targets.RemoveWhere(x => ShouldSkipTarget(x.Owner));
    }

    private bool ShouldSkipTarget(EntityUid uid)
    {
        if (_ghoulQuery.HasComp(uid))
            return true; // leave ghouls alone

        // ash heretics are immune
        return _heretic.TryGetHereticComponent(uid, out var heretic, out _) &&
               heretic.CurrentPath == HereticPath.Ash;
    }

    private bool TrySendBeam(Entity<FireBlastedComponent> origin)
    {
        // If the beam had already bounced at least once
        if (origin.Comp.HitEntities.Count > 0)
        {
            if (!TryComp(origin, out FlammableComponent? flammable))
                return false;

            if (!flammable.OnFire)
                return false;

            // Max bounces reached
            if (origin.Comp.HitEntities.Count >= origin.Comp.MaxBounces)
                return false;
        }

        var xform = Transform(origin);
        var pos = _transform.GetWorldPosition(xform);

        GetTargets(xform, origin.Comp.FireBlastRange);
        // Prioritize alive targets on fire, closest to origin
        var result = _targets
            .Select(x => (x, _flammableQuery.CompOrNull(x),
                (_transform.GetWorldPosition(x) - pos).LengthSquared()))
            .Where(x => x.Item2 != null && x.Item1.Owner != origin.Owner &&
                        !_fireBlastQuery.HasComp(x.Item1.Owner) &&
                        !origin.Comp.HitEntities.Contains(x.Item1.Owner))
            .OrderBy(x => x.Item1.Comp.CurrentState)
            .ThenByDescending(x => x.Item2!.OnFire)
            .ThenBy(x => x.Item3)
            .FirstOrNull();

        if (result == null)
            return false;

        var (target, flam, _) = result.Value;

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ref ev, true);

        var antimagic = ev.Cancelled;

        var time = origin.Comp.BeamTime;

        if (antimagic)
            time *= 2;

        if (!_status.TrySetStatusEffectDuration(target, FireBlastStatusEffect, time))
            return false;

        var fireBlasted = EnsureComp<FireBlastedComponent>(target);
        fireBlasted.HitEntities = new(origin.Comp.HitEntities);
        fireBlasted.HitEntities.Add(origin);
        fireBlasted.Damage = antimagic ? 0f : 2f;
        fireBlasted.MaxBounces = origin.Comp.MaxBounces;
        fireBlasted.BeamTime = origin.Comp.BeamTime;
        Dirty(target, fireBlasted);

        // Send beam from target to origin so that we can easier remove it if we only have access to target
        var data = new ComplexJointVisualsData(origin.Comp.FireBlastBeamDataId,
            origin.Comp.FireBlastBeamSprite,
            origin.Comp.FireBlastRange)
        {
            ReverseBeam = true,
        };
        _joint.CreateJoint(origin, target, data);

        _audio.PlayPvs(origin.Comp.Sound, xform.Coordinates);

        if (antimagic)
            return true;

        _flammable.AdjustFireStacks(target, origin.Comp.FireStacks, flam, true, origin.Comp.FireProtectionPenetration);

        _dmg.ChangeDamage(target.Owner, origin.Comp.FireBlastDamage, origin: origin, targetPart: TargetBodyPart.Vital);

        return true;
    }

    [SubscribeLocalEvent]
    private void BeamCollision(Entity<FireBlastedComponent> ent, ref ComplexJointCollisionEvent args)
    {
        if (args.Data.Id != ent.Comp.FireBlastBeamDataId)
            return;

        var otherEntity = args.Hit.HitEntity;

        if (!_mobQuery.HasComp(otherEntity))
            return;

        if (ShouldSkipTarget(otherEntity))
            return;

        if (_flammableQuery.TryComp(otherEntity, out var flam))
        {
            _flammable.AdjustFireStacks(otherEntity,
                ent.Comp.CollisionFireStacks,
                flam,
                true,
                ent.Comp.FireProtectionPenetration);
        }

        _dmg.ChangeDamage(otherEntity, ent.Comp.FireBlastBeamCollideDamage, interruptsDoAfters: false, targetPart: TargetBodyPart.Vital);
    }
}
