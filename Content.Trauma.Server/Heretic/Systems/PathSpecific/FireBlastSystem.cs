// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Physics;
using Content.Goobstation.Common.Religion;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class FireBlastSystem : SharedFireBlastSystem
{
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private EntityQuery<FlammableComponent> _flammableQuery = default!;
    [Dependency] private EntityQuery<GhoulComponent> _ghoulQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;

    private HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireBlastedComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(Entity<FireBlastedComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ClearBeamJoints((ent.Owner, ent.Comp));

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

            Dmg.TryChangeDamage(uid,
                origin.Comp.FireBlastBonusDamage * Body.GetVitalBodyPartRatio(uid),
                false,
                false,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll,
                canMiss: false);
        }
    }

    private void GetTargets(TransformComponent xform, float range)
    {
        _targets.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, range, _targets, flags: LookupFlags.Dynamic);
        _targets.RemoveWhere(ent =>
        {
            var uid = ent.Owner;
            if (_ghoulQuery.HasComp(uid))
                return true; // leave ghouls alone

            // ash heretics are immune
            return _heretic.TryGetHereticComponent(uid, out var heretic, out _) && heretic.CurrentPath == HereticPath.Ash;
        });
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
        var pos = Xform.GetWorldPosition(xform);

        GetTargets(xform, origin.Comp.FireBlastRange);
        // Prioritize alive targets on fire, closest to origin
        var result = _targets
            .Select(x => (x, _flammableQuery.CompOrNull(x),
                (Xform.GetWorldPosition(x) - pos).LengthSquared()))
            .Where(x => x.Item2 != null && x.Item1.Owner != origin.Owner &&
                !Status.HasEffectComp<FireBlastedStatusEffectComponent>(x.Item1.Owner) &&
                !origin.Comp.HitEntities.Contains(x.Item1.Owner))
            .OrderBy(x => x.Item1.Comp.CurrentState)
            .ThenByDescending(x => x.Item2!.OnFire)
            .ThenBy(x => x.Item3)
            .FirstOrNull();

        if (result == null)
            return false;

        var (target, flam, _) = result.Value;

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ev, true);

        var antimagic = ev.Cancelled;

        var time = origin.Comp.BeamTime;

        if (antimagic)
            time *= 2;

        if (!Status.TrySetStatusEffectDuration(target, FireBlastStatusEffect, time))
            return false;

        var fireBlasted = EnsureComp<FireBlastedComponent>(target);
        fireBlasted.HitEntities = new(origin.Comp.HitEntities);
        fireBlasted.HitEntities.Add(origin);
        fireBlasted.Damage = antimagic ? 0f : 2f;
        fireBlasted.MaxBounces = origin.Comp.MaxBounces;
        fireBlasted.BeamTime = origin.Comp.BeamTime;
        Dirty(target, fireBlasted);

        // Send beam from target to origin so that we can easier remove it if we only have access to target
        var beam = EnsureComp<ComplexJointVisualsComponent>(target);
        beam.Data[GetNetEntity(origin)] =
            new ComplexJointVisualsData(origin.Comp.FireBlastBeamDataId, origin.Comp.FireBlastBeamSprite);
        Dirty(target, beam);

        _audio.PlayPvs(origin.Comp.Sound, xform.Coordinates);

        if (antimagic)
            return true;

        _flammable.AdjustFireStacks(target, origin.Comp.FireStacks, flam, true, origin.Comp.FireProtectionPenetration);

        Dmg.TryChangeDamage(target.Owner,
            origin.Comp.FireBlastDamage * Body.GetVitalBodyPartRatio(target.Owner),
            origin: origin,
            targetPart: TargetBodyPart.All,
            splitDamage: SplitDamageBehavior.SplitEnsureAll,
            canMiss: false);

        return true;
    }

    protected override void BeamCollision(Entity<FireBlastedComponent> origin, EntityUid target)
    {
        base.BeamCollision(origin, target);

        var originPos = Xform.GetMapCoordinates(origin);
        var targetPos = Xform.GetMapCoordinates(target);

        var dir = originPos.Position - targetPos.Position;

        var ray = new CollisionRay(targetPos.Position, dir.Normalized(), (int) CollisionGroup.Opaque);
        var dist = MathF.Min(dir.Length(), origin.Comp.FireBlastRange);
        var result = _physics.IntersectRay(originPos.MapId, ray, dist, origin, false);

        foreach (var ent in result)
        {
            if (ent.HitEntity == target)
                continue;

            if (!_mobQuery.HasComp(ent.HitEntity))
                return;

            if (_ghoulQuery.HasComp(ent.HitEntity))
                continue;

            if (_heretic.TryGetHereticComponent(ent.HitEntity, out var heretic, out _) &&
                heretic.CurrentPath == HereticPath.Ash)
                continue;

            if (_flammableQuery.TryComp(ent.HitEntity, out var flam))
            {
                _flammable.AdjustFireStacks(ent.HitEntity,
                    origin.Comp.CollisionFireStacks,
                    flam,
                    true,
                    origin.Comp.FireProtectionPenetration);
            }

            Dmg.TryChangeDamage(ent.HitEntity,
                origin.Comp.FireBlastBeamCollideDamage * Body.GetVitalBodyPartRatio(ent.HitEntity),
                false,
                false,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll,
                canMiss: false);
        }
    }

    private void ClearBeamJoints(Entity<FireBlastedComponent, ComplexJointVisualsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        ent.Comp2.Data = ent.Comp2.Data.Where(x => x.Value.Id != ent.Comp1.FireBlastBeamDataId).ToDictionary();

        if (ent.Comp2.Data.Count == 0)
            RemComp(ent.Owner, ent.Comp2);
        else
            Dirty(ent.Owner, ent.Comp2);
    }
}
