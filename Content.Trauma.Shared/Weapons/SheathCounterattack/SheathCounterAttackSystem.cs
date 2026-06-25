// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.NPC;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Weapons.SheathCounterattack;

public sealed partial class SheathCounterAttackSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private RiposteeSystem _riposte = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;

    [Dependency] private EntityQuery<CounterAttackerComponent> _counterAttackerQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CounterAttackerComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction);

        Subs.SubscribeWithRelay<SheathCounterattackComponent, GetCounterAttackSheathEvent>(OnGetSheath,
            baseEvent: false,
            held: false);

        SubscribeLocalEvent<CombatModeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        SubscribeLocalEvent<CounterAttackingStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
    }

    private void OnRemove(Entity<CounterAttackingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        // Don't apply blocker status effect if this effect has ended early
        if (!TryComp(ent, out StatusEffectComponent? status) || status.EndEffectTime is not { } time ||
            time > _timing.CurTime)
            return;

        _status.TryAddStatusEffect(args.Target,
            ent.Comp.BlockStatusEffect,
            out _,
            ent.Comp.BlockEffectTime,
            TimeSpan.FromMilliseconds(1));
    }

    private void OnGetSheath(Entity<SheathCounterattackComponent> ent, ref GetCounterAttackSheathEvent args)
    {
        args.Sheath ??= ent;
    }

    private void OnGetAltVerbs(Entity<CombatModeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (user == ent.Owner || !_counterAttackerQuery.TryComp(user, out var comp) || !CanCounterAttack(user))
            return;

        if (_status.HasEffectComp<CounterAttackingStatusEffectComponent>(user) ||
            _status.HasEffectComp<BlockCounterAttackStatusEffectComponent>(user))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("counter-attack-verb"),
            Icon = comp.Icon,
            Priority = 10,
            Act = () =>
            {
                if (!CanCounterAttack(user)) // check again
                    return;

                if (GetSheathAndWeapon(user) is not { } tuple)
                    return;

                var (sheath, weapon) = tuple;

                if (!sheath.Comp.CanCounterNpc && HasComp<ActiveNPCComponent>(ent))
                {
                    _popup.PopupClient(Loc.GetString("counter-attack-fail-npc-message"), user, user);
                    return;
                }

                _status.TryAddStatusEffect(user,
                    sheath.Comp.StatusEffect,
                    out var effect,
                    sheath.Comp.CounterWindowTime);
                if (!TryComp(effect, out CounterAttackingStatusEffectComponent? counterComp))
                    return;

                counterComp.ActiveSheath = sheath;
                counterComp.ActiveWeapon = weapon;
                counterComp.Target = ent;
                counterComp.BlockEffectTime = sheath.Comp.BlockEffectTime;
                Dirty(effect.Value, counterComp);

                var userIdentity = Identity.Entity(user, EntityManager);
                var targetIdentity = Identity.Entity(ent.Owner, EntityManager, user);

                _popup.PopupPredicted(Loc.GetString("counter-attack-self-message",
                        ("target", targetIdentity)),
                    Loc.GetString("counter-attack-others-message",
                        ("user", userIdentity),
                        ("sheath", sheath.Owner)),
                        user,
                        user);

                _audio.PlayPredicted(sheath.Comp.CounterAttackingSound, user, user);
            },
        });
    }

    private bool CanCounterAttack(EntityUid user)
    {
        return _combat.IsInCombatMode(user) && _hands.GetHandCount(user) > 0 && _hands.GetActiveItem(user) == null;
    }

    private (Entity<SheathCounterattackComponent>, Entity<MeleeWeaponComponent>)? GetSheathAndWeapon(EntityUid user)
    {
        var ev = new GetCounterAttackSheathEvent();
        RaiseLocalEvent(user, ref ev);
        if (ev.Sheath is not { } sheath || _slots.GetItemOrNull(sheath, sheath.Comp.SlotId) is not { } weapon ||
            !TryComp(weapon, out MeleeWeaponComponent? melee) || melee.NextAttack >= _timing.CurTime)
            return null;

        return (sheath, (weapon, melee));
    }

    private void OnBeforeHarmfulAction(Entity<CounterAttackerComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        // This isn't predicted because it calls _riposte.CounterAttack which calls
        // SharedMeleeWeaponSystem.AttemptLightAttack that does melee lunge animation for all clients except for ent.Owner
        // (this inclures client session that has attacked this entity).
        // Predicting would result in lunge animation playing twice for args.User
        if (_net.IsClient || args.Cancelled || !args.CanRiposte || !CanCounterAttack(ent.Owner) ||
            !_blocker.CanInteract(ent.Owner, args.Target) ||
            TryComp(args.Used, out MeleeWeaponComponent? melee) && !melee.CanBeParried)
            return;

        if (!_status.TryEffectsWithComp<CounterAttackingStatusEffectComponent>(ent, out var effects) ||
            effects.Count == 0)
            return;

        var (uid, comp, _) = effects.First();

        if (comp.Target != args.User || GetSheathAndWeapon(ent) is not { } tuple)
            return;

        var (sheath, weapon) = tuple;

        if (sheath != comp.ActiveSheath || weapon != comp.ActiveWeapon)
            return;

        if (!_slots.TryGetSlot(sheath, sheath.Comp.SlotId, out var slot) ||
            !_slots.TryEjectToHands(sheath, slot, ent, doAfter: false))
            return;

        weapon.Comp.NextAttack = TimeSpan.Zero;
        Dirty(weapon);

        if (_riposte.CounterAttack(weapon, ent, args.User, sheath.Comp.CounterAttackSuccessSound, null))
        {
            var dmg = new DamageSpecifier()
            {
                DamageDict = weapon.Comp.Damage.DamageDict.ToDictionary(x => x.Key, x => x.Value * sheath.Comp.ExtraDamageMultiplier),
                ArmorPenetration = sheath.Comp.ExtraArmorPenetration,
                WoundSeverityMultipliers = sheath.Comp.ExtraWoundSeverityMultipliers,
            };

            _dmg.ChangeDamage(args.User, dmg, origin: ent, canBeCancelled: false, canMiss: false);
        }

        QueueDel(uid);
        args.Cancelled = true;

        RaiseNetworkEvent(new RiposteUsedEvent(GetNetEntity(ent.Owner),
                GetNetEntity(args.User),
                GetNetEntity(weapon.Owner),
                sheath.Comp.CounterAttackSuccessSound,
                null),
            ent.Owner);
    }
}
