// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpaceWhale;
using Content.Medical.Shared.Wounds;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Nutrition;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Wizard.SanguineStrike;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Flesh;

public sealed class LordOfTheNightSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly WoundSystem _wound = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedSanguineStrikeSystem _lifesteal = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly SharedTransformSystem _transfrm = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effect = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    [Dependency] private readonly EntityQuery<WoundableComponent> _woundableQuery = default!;

    private readonly HashSet<Entity<MobStateComponent>> _lookMobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LordOfTheNightComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LordOfTheNightComponent, MeleeHitEvent>(OnHit);
        SubscribeLocalEvent<LordOfTheNightComponent, FullyAteEvent>(OnFullyAte);
        SubscribeLocalEvent<LordOfTheNightComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<LordOfTheNightComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<LordOfTheNightComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<LordOfTheNightComponent, GetTailedEntitySegmentCountEvent>(OnGetSegmentCount);
        SubscribeLocalEvent<LordOfTheNightComponent, StartCollideEvent>(OnCollide);

        SubscribeLocalEvent<TailedEntitySegmentComponent, DamageChangedEvent>(OnSegmentDamageChanged);
    }

    private void OnMapInit(Entity<LordOfTheNightComponent> ent, ref MapInitEvent args)
    {
        _lookMobs.Clear();
        _look.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.MadnessRange, _lookMobs);
        foreach (var (uid, mob) in _lookMobs)
        {
            if (mob.CurrentState != MobState.Alive)
                continue;

            if (_heretic.IsHereticOrGhoul(uid))
                continue;

            if (!_examine.InRangeUnOccluded(uid, ent, ent.Comp.MadnessRange))
                continue;

            _effect.ApplyEffects(uid, ent.Comp.MadnessEffects, 1f, ent);
        }
    }

    private void OnCollide(Entity<LordOfTheNightComponent> ent, ref StartCollideEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var other = args.OtherEntity;
        var xform = Transform(other);

        if (xform.Anchored)
        {
            if (!_whitelist.IsValid(ent.Comp.UnanchorWhitelist, other))
                return;

            _transfrm.Unanchor(other);
        }

        if (!args.OtherFixture.Hard || args.OurBody.LinearVelocity == Vector2.Zero ||
            _whitelist.IsValid(ent.Comp.PushBlacklist, other))
            return;

        var force = args.OurBody.LinearVelocity * args.OurBody.Mass * args.OtherBody.Mass *
                    ent.Comp.ForceMultiplier;

        _physics.ApplyForce(other, force, body: args.OtherBody);
    }

    private void OnGetSegmentCount(Entity<LordOfTheNightComponent> ent, ref GetTailedEntitySegmentCountEvent args)
    {
        args.Amount = GetSegmentCount((ent, null, ent.Comp));
    }

    private void OnMindRemoved(Entity<LordOfTheNightComponent> ent, ref MindRemovedMessage args)
    {
        if (_action.TryGetActionById(args.Mind, ent.Comp.TransformAction, out var action))
            _action.StartUseDelay(action.Value.AsNullable());

        if (!TryComp(args.Mind, out FleshHereticMindComponent? fleshMind))
            return;

        fleshMind.WormSustainedDamage = _dmg.GetAllDamage(ent.Owner);
        Dirty(args.Mind, fleshMind);
    }

    private void OnMindAdded(Entity<LordOfTheNightComponent> ent, ref MindAddedMessage args)
    {
        if (ent.Comp.HereticInitialized)
            return;

        if (!TryComp(args.Mind, out FleshHereticMindComponent? fleshMind))
            return;

        ent.Comp.HereticInitialized = true;
        _dmg.ChangeDamage(ent.Owner, fleshMind.WormSustainedDamage, true, false);
    }

    private void OnSegmentDamageChanged(Entity<TailedEntitySegmentComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } dmg || !Exists(ent.Comp.Head) ||
            !HasComp<LordOfTheNightComponent>(ent.Comp.Head.Value))
            return;

        _dmg.ChangeDamage(ent.Comp.Head.Value, dmg, true, false, args.Origin);
    }

    private void OnDamageChanged(Entity<LordOfTheNightComponent> ent, ref DamageChangedEvent args)
    {
        var segmentCount = GetSegmentCount((ent, args.Damageable, ent.Comp));
        var ev = new UpdateTailedEntitySegmentCountEvent(segmentCount);
        RaiseLocalEvent(ent, ref ev);
    }

    private int GetSegmentCount(Entity<DamageableComponent?, LordOfTheNightComponent> ent)
    {
        if (!_threshold.TryGetDeadThreshold(ent.Owner, out var threshold))
            return 0;

        var total = _threshold.CheckVitalDamage(ent);
        var health = FixedPoint2.Max(FixedPoint2.Zero, threshold.Value - total);
        var segmentCount = Math.Max(0, (health / ent.Comp2.HealthPerSegment).Int() - 1);
        return segmentCount;
    }

    private void OnFullyAte(Entity<LordOfTheNightComponent> ent, ref FullyAteEvent args)
    {
        if (!_whitelist.IsValid(ent.Comp.ArmWhitelist, args.Food))
            return;

        _lifesteal.LifeSteal(ent.Owner, ent.Comp.HealPerArm);
    }

    private void OnHit(Entity<LordOfTheNightComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var netEnt = GetNetEntity(ent);

        foreach (var hit in args.HitEntities)
        {
            if (_mobState.IsAlive(hit) &&
                !SharedRandomExtensions.PredictedProb(_timing, ent.Comp.ArmDelimbChance, netEnt, GetNetEntity(hit)))
                continue;

            var arm = _body.GetOrgan(hit, ent.Comp.ArmLeft) ?? _body.GetOrgan(hit, ent.Comp.ArmRight);

            if (arm is { } armEnt && _woundableQuery.TryComp(armEnt, out var woundable) &&
                woundable.ParentWoundable is { } parent)
                _wound.AmputateWoundable(parent, armEnt, woundable, args.User);
        }
    }
}
