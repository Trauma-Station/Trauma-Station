// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class TouchSpellSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TouchSpellComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TouchSpellComponent, MeleeHitEvent>(OnMelee);

        SubscribeLocalEvent<TouchSpellEvent>(OnTouchSpell);
    }

    private void OnTouchSpell(TouchSpellEvent args)
    {
        var ev = new BeforeTouchSpellAbilityUsedEvent(args);
        RaiseLocalEvent(args.Performer, ref ev);
        if (ev.Cancelled)
            return;

        var spawned = GetTouchSpell(args.Performer, args, ev.TouchSpell);
        if (spawned == null)
            return;

        var spell = Comp<TouchSpellComponent>(spawned.Value);
        spell.Action = args.Action;
        Dirty(spawned.Value, spell);

        var ev2 = new AfterTouchSpellAbilityUsedEvent(spawned.Value);
        RaiseLocalEvent(args.Action, ref ev2);
    }

    private void OnMelee(Entity<TouchSpellComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (args.HitEntities.Count == 1)
        {
            args.Handled = TryUseTouchSpell(ent, args.User, args.HitEntities[0]);
            return;
        }

        UseTouchSpellMultiTarget(ent, args.User, args.HitEntities);
        args.Handled = true;
    }

    private void OnAfterInteract(Entity<TouchSpellComponent> ent, ref AfterInteractEvent args)
    {
        if (args is not { Handled: false, CanReach: true, Target: { } target })
            return;

        args.Handled = TryUseTouchSpell(ent, args.User, target);
    }

    public bool TryUseTouchSpell(Entity<TouchSpellComponent> ent, EntityUid user, EntityUid target)
    {
        if (!CanUseTouchSpell(ent, user, target))
            return false;

        if (UseTouchSpell(ent, user, target, out var cooldownOverride))
            InvokeTouchSpell(ent.AsNullable(), user, cooldownOverride);

        return true;
    }

    public bool CanUseTouchSpell(Entity<TouchSpellComponent> ent, EntityUid user, EntityUid target)
    {
        if (!ent.Comp.CanUseOnSelf && user == target)
            return false;

        if (!_whitelist.CheckBoth(target, ent.Comp.TargetBlacklist, ent.Comp.TargetWhitelist))
            return false;

        var ev = new TouchSpellAttemptEvent(user, target);
        RaiseLocalEvent(ent, ref ev);
        return !ev.Cancelled;
    }

    public void UseTouchSpellMultiTarget(Entity<TouchSpellComponent> ent,
        EntityUid user,
        IEnumerable<EntityUid> targets,
        TimeSpan? cooldownOverride = null)
    {
        var cooldown = cooldownOverride ?? ent.Comp.Cooldown;
        var invoke = false;

        foreach (var target in targets)
        {
            if (!CanUseTouchSpell(ent, user, target))
                continue;

            if (!UseTouchSpell(ent, user, target, out var cdOverride))
                continue;

            invoke = true;
            if (cdOverride != null && cdOverride > cooldown)
                cooldown = cdOverride.Value;
        }

        if (invoke)
            InvokeTouchSpell(ent.AsNullable(), user, cooldown);
    }

    public bool UseTouchSpell(Entity<TouchSpellComponent> ent,
        EntityUid user,
        EntityUid target,
        out TimeSpan? cooldownOverride)
    {
        cooldownOverride = null;
        if (!ent.Comp.BypassNullrod)
        {
            var beforeEv = new BeforeCastTouchSpellEvent(target);
            RaiseLocalEvent(target, beforeEv, true);
            if (beforeEv.Cancelled)
                return true;
        }

        var ev = new TouchSpellUsedEvent(user, target);
        RaiseLocalEvent(ent, ref ev);
        cooldownOverride = ev.CooldownOverride;
        return ev.Invoke;
    }

    public void InvokeTouchSpell(Entity<TouchSpellComponent?> ent,
        EntityUid user,
        TimeSpan? cooldownOverride = null,
        bool predicted = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        InvokeTouchSpell(user,
            ent.Comp.Action,
            ent.Comp.Sound,
            ent.Comp.Speech,
            cooldownOverride ?? ent.Comp.Cooldown,
            predicted);

        if (cooldownOverride != TimeSpan.Zero)
            PredictedQueueDel(ent);
    }

    public void InvokeTouchSpell(EntityUid user,
        EntityUid? action,
        SoundSpecifier? sound,
        LocId? speech,
        TimeSpan cooldown,
        bool predicted = true)
    {
        _audio.PlayPredicted(sound, user, predicted ? user : null);

        if (speech != null)
            _chat.TrySendInGameICMessage(user, Loc.GetString(speech), InGameICChatType.Speak, false);

        if (cooldown > TimeSpan.Zero && Exists(action))
            _actions.SetCooldown(action.Value, cooldown);

        var ev = new UserInvokeTouchSpellEvent();
        RaiseLocalEvent(user, ref ev);
    }

    public EntityUid? FindTouchSpell(EntityUid user, EntityWhitelist whitelist)
    {
        if (!TryComp(user, out HandsComponent? hands) || hands.Hands.Count < 1)
            return null;

        foreach (var held in _hands.EnumerateHeld((user, hands)))
        {
            if (_whitelist.IsWhitelistPass(whitelist, held))
                return held;
        }

        return null;
    }

    private EntityUid? GetTouchSpell(EntityUid ent, TouchSpellEvent args, EntProtoId? touchSpellOverride)
    {
        if (FindTouchSpell(ent, args.TouchSpellWhitelist) is { } spell)
        {
            PredictedQueueDel(spell);
            return null;
        }

        if (!_hands.TryGetEmptyHand(ent, out var emptyHand))
        {
            if (args.SpecialEvent is not { } specialEv)
                return null;

            specialEv.Invoke = false;
            RaiseLocalEvent(args.Performer, (object) specialEv);
            if (specialEv.Invoke)
                InvokeTouchSpell(args.Performer, args.Action, specialEv.Sound, specialEv.Speech, specialEv.Cooldown);
            return null;
        }

        var touch = PredictedSpawnAtPosition(touchSpellOverride ?? args.TouchSpell, Transform(ent).Coordinates);

        if (_hands.TryPickup(ent, touch, emptyHand, animate: false))
            return touch;

        PredictedQueueDel(touch);
        return null;
    }
}
