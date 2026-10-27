// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.EntityConditions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Multihit;

public sealed partial class MultihitSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<MeleeWeaponComponent> _meleeQuery = default!;
    [Dependency] private EntityQuery<ActiveMultihitComponent> _activeQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ActiveMultihitComponent, MeleeWeaponComponent>();
        while (query.MoveNext(out var uid, out var active, out var melee))
        {
            if (active.LastAttack == null && active.QueuedAttacks.Count == 0)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            if (curTime < (active.LastAttack ?? active.QueuedAttacks.Peek()).AttackTime)
                continue;

            if (ResolveUser((uid, active)) is not { } user)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            QueuedMultihitAttack attack;

            // This is an endless war with prediction
            if (_net.IsClient)
            {
                active.LastAttack ??= active.QueuedAttacks.Dequeue();
                attack = active.LastAttack;
            }
            else
            {
                active.LastAttack = null;
                attack = active.QueuedAttacks.Dequeue();
                RaiseNetworkEvent(new ResetMultihitLastAttackEvent(GetNetEntity(uid)), user);
            }

            PerformFollowupAttack((uid, active, melee), attack, user);
        }
    }

    [SubscribeNetworkEvent]
    private void OnReset(ResetMultihitLastAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession != _player.LocalSession || !TryGetEntity(msg.Weapon, out var weapon) ||
            !_activeQuery.TryComp(weapon.Value, out var active))
            return;

        active.LastAttack = null;
    }

    private EntityUid? ResolveUser(Entity<ActiveMultihitComponent> ent)
    {
        if (ent.Comp.User is not { } user || TerminatingOrDeleted(user) || !_hands.IsHolding(user, ent))
            return null;

        return user;
    }

    private void PerformFollowupAttack(Entity<ActiveMultihitComponent, MeleeWeaponComponent> ent,
        QueuedMultihitAttack attack,
        EntityUid user)
    {
        var (uid, active, melee) = ent;

        active.NextDamageMultiplier = attack.DamageMultiplier;

        if (attack.IsHeavyAttack)
            TryPerformMultihitHeavyAttack((uid, melee), user);
        else if (TryGetEntity(attack.Target, out var target))
            TryPerformMultihitLightAttack((uid, melee), target.Value, user);

        active.NextDamageMultiplier = 1f;
    }

    private bool TryPerformMultihitLightAttack(Entity<MeleeWeaponComponent> ent, EntityUid target, EntityUid user)
    {
        var (uid, weapon) = ent;

        if (TerminatingOrDeleted(target))
            return false;

        var inCombat = _combatMode.IsInCombatMode(user);
        if (!inCombat)
            _combatMode.SetInCombatMode(user, true);
        var result = _melee.AttemptLightAttack(user, uid, weapon, target);
        if (!inCombat)
            _combatMode.SetInCombatMode(user, false);
        return result;
    }

    private bool TryPerformMultihitHeavyAttack(Entity<MeleeWeaponComponent> ent, EntityUid user)
    {
        var (uid, weapon) = ent;

        var xform = Transform(user);
        var userCoords = _transform.GetMapCoordinates(user, xform);
        var angle = Transform(user).LocalRotation;
        var direction = angle.ToWorldVec();
        var entities = _melee.ArcRayCast(userCoords.Position,
                angle,
                weapon.Angle,
                weapon.Range,
                xform.MapID,
                user)
            .ToList();

        var inCombat = _combatMode.IsInCombatMode(user);
        if (!inCombat)
            _combatMode.SetInCombatMode(user, true);
        var result = _melee.AttemptHeavyAttack(user,
            uid,
            weapon,
            entities,
            _transform.ToCoordinates(userCoords.Offset(direction)));
        if (!inCombat)
            _combatMode.SetInCombatMode(user, false);
        return result;
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<MultihitComponent> ent, ref MeleeHitEvent args)
    {
        var user = args.User;
        var hitEntities = args.HitEntities;
        var direction = args.Direction;

        if (_net.IsClient && _player.LocalEntity != user)
            return;

        if (!args.IsHit || args.Weapon == user)
            return;

        if (direction == null)
        {
            if (hitEntities.Count == 0)
                return;

            if (hitEntities[0] == user)
                return;
        }

        if (_activeQuery.HasComp(ent))
            return;

        if (!_condition.TryConditions(user, ent.Comp.UserConditions))
            return;

        var curTime = _timing.CurTime;
        var delay = ent.Comp.MultihitDelay;
        var penalty = TimeSpan.Zero;

        var attackIndex = 1;
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (ent.Comp.MaxFollowUpAttacks > 0 && attackIndex > ent.Comp.MaxFollowUpAttacks)
                break;

            if (!TryMultihitAttack(held))
                continue;

            delay += ent.Comp.MultihitDelay;
            penalty += ent.Comp.DelayPenalty;
            attackIndex++;
        }

        if (penalty <= TimeSpan.Zero || !_meleeQuery.TryComp(ent, out var melee))
            return;

        melee.NextAttack += penalty;

        return;

        bool TryMultihitAttack(EntityUid weapon)
        {
            if (weapon == ent.Owner)
                return false;

            if (_whitelist.IsWhitelistFail(ent.Comp.MultihitWhitelist, weapon))
                return false;

            if (!_meleeQuery.HasComp(weapon))
                return false;

            var active = EnsureComp<ActiveMultihitComponent>(weapon);

            if (active.User != null && active.User != user)
                active.QueuedAttacks.Clear();

            active.User = user;
            var multiplier = ent.Comp.DamageMultiplier;

            var attack = new QueuedMultihitAttack
            {
                AttackTime = curTime + delay,
                // Damage multiplier gets lower the longer your attack chain is
                DamageMultiplier = multiplier < 1f ? MathF.Pow(multiplier, attackIndex) : multiplier,
                Target = hitEntities.Count == 0 ? null : GetNetEntity(hitEntities[0]),
                IsHeavyAttack = direction != null,
            };

            active.QueuedAttacks.Enqueue(attack);
            active.LastAttack = null;
            Dirty(weapon, active);
            return true;
        }
    }
}
