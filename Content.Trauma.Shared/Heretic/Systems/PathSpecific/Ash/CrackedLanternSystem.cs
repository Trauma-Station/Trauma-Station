// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Wizard.Projectile;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

public sealed class CrackedLanternSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly NpcFactionSystem _npc = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    [Dependency] private readonly EntityQuery<CrackedLanternComponent> _lanterQuery = default!;

    private readonly HashSet<Entity<NpcFactionMemberComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrackedLanternComponent, GotEquippedHandEvent>(OnEquipHand);
        SubscribeLocalEvent<CrackedLanternComponent, GotUnequippedHandEvent>(OnUnequipHand);
        SubscribeLocalEvent<CrackedLanternComponent, MeleeHitEvent>(OnMelee);

        SubscribeLocalEvent<CrackedLanternSummonComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<CrackedLanternSummonComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnMelee(Entity<CrackedLanternComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0 || !_heretic.IsHereticOrGhoul(args.User))
            return;

        var target = args.HitEntities[0];
        if (args.User == target || !_mob.IsAlive(target))
            return;

        if (!_delay.TryResetDelay(ent, true))
            return;

        var coords = CalculateCoordinates(target);

        if (!Exists(ent.Comp.Summoned))
            SpawnHint(ent, args.User).Comp.TargetCoords = coords;
        else
            CompOrNull<CrackedLanternSummonComponent>(ent.Comp.Summoned)?.TargetCoords = coords;
    }

    private void OnBeforeDamageChanged(Entity<CrackedLanternSummonComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.TargetCoords == ent.Comp.UserCoords)
            args.Cancelled = true;
    }

    private void OnDamageChanged(Entity<CrackedLanternSummonComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.TargetCoords == ent.Comp.UserCoords)
            return;
        if (_threshold.CheckVitalDamage((ent, args.Damageable)) < ent.Comp.Health)
            return;

        _dmg.SetAllDamage((ent, args.Damageable), FixedPoint2.Zero);
        ent.Comp.TargetCoords = ent.Comp.UserCoords;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        var query =
            EntityQueryEnumerator<CrackedLanternSummonComponent, MeleeWeaponComponent, PhysicsComponent,
                TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var melee, out var body, out var xform))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;

            if (!Exists(comp.Lantern) || !_lanterQuery.TryComp(comp.Lantern.Value, out var lantern))
            {
                QueueDel(uid);
                continue;
            }

            var lanternCoords = comp.Lantern.Value.ToCoordinates();

            if (!xform.Coordinates.TryDistance(EntityManager, _transform, lanternCoords, out var dist) ||
                dist > comp.MaxDisappearDistance)
            {
                QueueDel(uid);
                lantern.Summoned = null;
                Dirty(comp.Lantern.Value, lantern);
                continue;
            }

            if (dist > comp.MaxDistance)
                comp.TargetCoords = comp.UserCoords;

            var followingUser = comp.TargetCoords == comp.UserCoords;

            if (comp.TargetCoords is not { } coords ||
                !coords.TryDelta(EntityManager, _transform, xform.Coordinates, out var delta) ||
                comp.ShouldDespawn && followingUser ||
                !_mob.IsAlive(coords.EntityId))
            {
                if (followingUser)
                {
                    var pos = _transform.GetWorldPosition(xform);
                    SpawnTrail(comp, pos, lanternCoords);
                    QueueDel(uid);
                    lantern.Summoned = null;
                    Dirty(comp.Lantern.Value, lantern);
                    continue;
                }

                comp.TargetCoords = comp.UserCoords;
                continue;
            }

            if (melee.NextAttack <= now)
                AttackPeople((uid, xform, comp, melee, null));

            var len = delta.Length();
            if (len < comp.DistThreshold)
                continue;

            Vector2 vec;
            if (len * comp.SpeedMultiplier > comp.MaxSpeed)
                vec = delta * comp.MaxSpeed / len;
            else
                vec = delta * comp.SpeedMultiplier;

            _physics.SetLinearVelocity(uid, vec, body: body);

            var realTargetCoords = new EntityCoordinates(coords.EntityId, Vector2.Zero);
            if (realTargetCoords.TryDelta(EntityManager, _transform, xform.Coordinates, out var delta2))
                _transform.SetWorldRotation(xform, delta2.ToWorldAngle());
        }
    }

    private void OnUnequipHand(Entity<CrackedLanternComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!Exists(ent.Comp.Summoned) || !TryComp(ent.Comp.Summoned, out CrackedLanternSummonComponent? comp))
            return;

        comp.ShouldDespawn = true;
    }

    private void OnEquipHand(Entity<CrackedLanternComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_mob.IsAlive(args.User))
            return;

        if (Exists(ent.Comp.Summoned))
        {
            if (TryComp(ent.Comp.Summoned.Value, out CrackedLanternSummonComponent? summon))
            {
                summon.ShouldDespawn = false;
                return;
            }

            PredictedQueueDel(ent.Comp.Summoned.Value);
            ent.Comp.Summoned = null;
        }

        SpawnHint(ent, args.User);
    }

    private Entity<CrackedLanternSummonComponent> SpawnHint(Entity<CrackedLanternComponent> ent, EntityUid user)
    {
        var coords = CalculateCoordinates(user);
        var uid = PredictedSpawnAtPosition(ent.Comp.SummonProto, coords);
        var comp = EnsureComp<CrackedLanternSummonComponent>(uid);
        comp.Lantern = ent;
        comp.TargetCoords = coords;
        comp.UserCoords = coords;

        ent.Comp.Summoned = uid;
        Dirty(ent);

        return (uid, comp);
    }

    private static EntityCoordinates CalculateCoordinates(EntityUid uid)
    {
        return uid.ToCoordinates(new Vector2(0f, 0.8f));
    }

    private void SpawnTrail(CrackedLanternSummonComponent comp,
        Vector2 trailPos,
        EntityCoordinates targetCoords)
    {
        _audio.PlayPvs(comp.TrailSound, targetCoords);
        var effect = SpawnAttachedTo(comp.TrailEffect, targetCoords);
        if (!TryComp(effect, out TrailComponent? trail))
            return;
        trail.SpawnPosition = trailPos;
        trail.Sprite = comp.TrailSprite;
        Dirty(effect, trail);
    }

    private void AttackPeople(
        Entity<TransformComponent, CrackedLanternSummonComponent, MeleeWeaponComponent, CombatModeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp4))
            return;

        var forceAttack = ent.Comp2.TargetCoords?.EntityId;
        if (forceAttack == ent.Comp2.UserCoords?.EntityId)
            forceAttack = null;

        _targets.Clear();

        _look.GetEntitiesInRange(ent.Comp1.Coordinates, ent.Comp3.Range, _targets, LookupFlags.Dynamic);
        _targets.RemoveWhere(x =>
            !_mob.IsAlive(x) || forceAttack != x && _npc.IsEntityFriendly(ent.Owner, x.AsNullable()));

        _combat.SetInCombatMode(ent, true, ent.Comp4);

        if (forceAttack is { } uid && _targets.RemoveWhere(x => uid == x.Owner) > 0)
        {
            if (_melee.InRange(ent, uid, ent.Comp3.Range, null, out _) &&
                _melee.AttemptLightAttack(ent, ent, ent.Comp3, uid))
                return;
        }

        if (_targets.Count == 0)
            return;

        var list = _targets.ToList();
        _random.Shuffle(list);
        foreach (var target in list)
        {
            if (_melee.InRange(ent, target, ent.Comp3.Range, null, out _) &&
                _melee.AttemptLightAttack(ent, ent, ent.Comp3, target))
                return;
        }
    }
}
