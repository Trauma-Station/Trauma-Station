// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public sealed partial class RiposteeSystem : EntitySystem
{
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiposteeComponent, BeforeHarmfulActionEvent>(OnHarmAttempt,
            before: new[] { typeof(SharedHereticAbilitySystem) });

        SubscribeNetworkEvent<RiposteUsedEvent>(OnRiposteUsed);
    }

    private void OnRiposteUsed(RiposteUsedEvent ev)
    {
        if (_net.IsServer)
            return;

        if (!TryGetEntity(ev.User, out var user) || !TryGetEntity(ev.Target, out var target) ||
            !TryGetEntity(ev.Weapon, out var weapon))
            return;

        if (_player.LocalEntity != user.Value)
            return;

        if (!TryComp(weapon.Value, out MeleeWeaponComponent? melee))
            return;

        CounterAttack((weapon.Value, melee),
            user.Value,
            target.Value,
            ev.Sound,
            ev.Message);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var eqe = EntityQueryEnumerator<RiposteeComponent>();
        while (eqe.MoveNext(out var uid, out var rip))
        {
            if (!uid.IsValid())
                continue;

            foreach (var data in rip.Data.Values)
            {
                if (data.Cooldown <= 0f)
                {
                    data.CanRiposte = true;
                    continue;
                }

                if (data.CanRiposte)
                    continue;

                data.Timer -= frameTime;

                if (data.Timer > 0)
                    continue;

                data.Timer = data.Cooldown;

                data.CanRiposte = true;
                if (data.RiposteAvailableMessage != null)
                    _popup.PopupEntity(Loc.GetString(data.RiposteAvailableMessage), uid, uid);
            }
        }
    }

    private void OnHarmAttempt(Entity<RiposteeComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled || !args.CanRiposte)
            return;

        if (_net.IsClient)
            return;

        if (TryComp(args.Used, out MeleeWeaponComponent? usedMelee) && !usedMelee.CanBeParried)
            return;

        if (_mobState.IsIncapacitated(ent))
            return;

        foreach (var data in ent.Comp.Data.Values)
        {
            if (!data.CanRiposte)
                continue;

            if (args.User == ent.Owner)
                continue;

            if (data.CanRiposteEvent != null)
            {
                var ev = data.CanRiposteEvent;
                ev.Handled = false;
                RaiseLocalEvent(ent, (object) ev);
                if (!ev.Handled)
                    continue;
            }

            if (!data.CanRiposteWhileProne && _standing.IsDown(ent.Owner))
                continue;

            if (data.RiposteChance is > 0f and < 1f)
            {
                if (!_random.Prob(data.RiposteChance))
                    continue;
            }

            Entity<MeleeWeaponComponent>? weapon = null;
            if (data.RequiresWeapon)
            {
                foreach (var held in _hands.EnumerateHeld(ent.Owner))
                {
                    if (_whitelist.IsWhitelistPassOrNull(data.WeaponWhitelist, held) &&
                        TryComp(held, out MeleeWeaponComponent? melee))
                        weapon = (held, melee);
                }
            }
            else
            {
                if (TryComp(ent.Owner, out MeleeWeaponComponent? melee)
                    && _hands.TryGetEmptyHand(ent.Owner, out _))
                    weapon = (ent.Owner, melee);
            }

            if (weapon == null)
                continue;

            if (!_blocker.CanAttack(ent, args.User, weapon.Value))
                continue;

            args.Cancelled = true;

            if (data.Cooldown > 0f)
                data.CanRiposte = false;

            if (CounterAttack(weapon.Value, ent, args.User, data.RiposteSound, data.RiposteUsedMessage))
            {
                if (data.StunTime > TimeSpan.Zero)
                    _stun.TryUpdateParalyzeDuration(args.User, data.StunTime);

                if (data.KnockdownTime > TimeSpan.Zero)
                    _stun.TryKnockdown(args.User, data.KnockdownTime);
            }

            RaiseNetworkEvent(new RiposteUsedEvent(GetNetEntity(ent.Owner),
                    GetNetEntity(args.User),
                    GetNetEntity(weapon.Value.Owner),
                    data.RiposteSound,
                    data.RiposteUsedMessage),
                ent.Owner);

            break;
        }
    }

    public bool CounterAttack(Entity<MeleeWeaponComponent> weapon,
        EntityUid user,
        EntityUid target,
        SoundSpecifier? sound,
        LocId? message)
    {
        var nextAttack = weapon.Comp.NextAttack;
        weapon.Comp.NextAttack = TimeSpan.Zero;

        var inCombat = _combatMode.IsInCombatMode(user);
        if (!inCombat)
            _combatMode.SetInCombatMode(user, true);

        var result = _melee.AttemptLightAttack(user, weapon.Owner, weapon.Comp, target, false) && _net.IsServer &&
                     _melee.InRange(user,
                         target,
                         weapon.Comp.Range,
                         CompOrNull<ActorComponent>(user)?.PlayerSession,
                         out _);

        if (!inCombat)
            _combatMode.SetInCombatMode(user, false);

        weapon.Comp.NextAttack = nextAttack;
        Dirty(weapon);

        if (_net.IsClient && _player.LocalEntity == target)
            return result;

        _audio.PlayPredicted(sound, user, user);

        if (message != null)
            _popup.PopupClient(Loc.GetString(message), user, user);

        return result;
    }
}

[Serializable, NetSerializable]
public sealed class RiposteUsedEvent(
    NetEntity user,
    NetEntity target,
    NetEntity weapon,
    SoundSpecifier? sound,
    LocId? message)
    : EntityEventArgs
{
    public NetEntity User = user;

    public NetEntity Target = target;

    public NetEntity Weapon = weapon;

    public SoundSpecifier? Sound = sound;

    public LocId? Message = message;
}
