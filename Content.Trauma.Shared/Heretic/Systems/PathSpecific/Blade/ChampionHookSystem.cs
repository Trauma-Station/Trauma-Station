// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public sealed partial class ChampionHookSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChampionHookComponent, EventHereticToggleChampionHook>(OnHookToggle);
        SubscribeLocalEvent<ChampionHookComponent, ComboAttackPerformedEvent>(OnAttack);
        SubscribeLocalEvent<ChampionHookComponent, BeforeSpawnPullingVirtualItemsEvent>(OnVirtualItems);
        SubscribeLocalEvent<ChampionHookComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ChampionHookComponent, MeleeAttackEvent>(OnMelee);
        SubscribeLocalEvent<ChampionHookComponent, GetGrabMovespeedEvent>(OnGetMovespeed);
        SubscribeLocalEvent<ChampionHookComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChampionHookComponent, PullStoppedMessage>(OnHookStopped);

        SubscribeLocalEvent<HereticBladeComponent, GotUnequippedHandEvent>(OnUnequipHand);

        SubscribeLocalEvent<ChampionHookedComponent, CanStandWhileImmobileEvent>(OnImmobileStand);
        SubscribeLocalEvent<ChampionHookedComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<ChampionHookedComponent, PullStoppedMessage>(OnHookedStopped);
        SubscribeLocalEvent<ChampionHookedComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<ChampionHookedComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction);
    }

    private void OnShutdown(Entity<ChampionHookComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Comp.Action))
            return;

        _action.SetToggled(ent.Comp.Action, false);
    }

    private void OnUnequipHand(Entity<HereticBladeComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!TryComp(args.User, out ChampionHookComponent? hook) || hook.Weapon != ent.Owner ||
            hook.HookedMob is not { } hooked || !TryComp(hooked, out PullableComponent? pullable))
            return;

        _pulling.TryStopPull(hooked, pullable, args.User, true);
    }

    private void OnImmobileStand(Entity<ChampionHookedComponent> ent, ref CanStandWhileImmobileEvent args)
    {
        args.CanStand = true;
    }

    private void OnStand(Entity<ChampionHookedComponent> ent, ref StoodEvent args)
    {
        _pulling.StopAllPulls(ent, stopPuller: false);
    }

    private void OnGetMovespeed(Entity<ChampionHookComponent> ent, ref GetGrabMovespeedEvent args)
    {
        if (ent.Comp.HookedMob != null)
            args.Speed += ent.Comp.MovespeedBuff;
    }

    private void OnMelee(Entity<ChampionHookComponent> ent, ref MeleeAttackEvent args)
    {
        if (ent.Comp.HookedMob == null || args.Weapon == ent.Comp.Weapon ||
            !HasComp<HereticBladeComponent>(args.Weapon) ||
            !_heretic.TryGetHereticComponent(ent.Owner, out var heretic, out _) || heretic is not
                { PathStage: >= 7, CurrentPath: HereticPath.Blade } ||
            !TryComp(args.Weapon, out MeleeWeaponComponent? weapon))
            return;

        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate * ent.Comp.OffhandAttackSpeedBuff;
        Dirty(args.Weapon, weapon);
    }

    private void OnAttackAttempt(Entity<ChampionHookComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Comp.HookedMob is not { } hooked || args.Weapon is not { } weapon || weapon != ent.Comp.Weapon)
            return;

        args.Cancel();
    }

    private void OnHookStopped(Entity<ChampionHookComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner != args.PullerUid)
            return;

        ent.Comp.Weapon = null;
        ent.Comp.HookedMob = null;
        Dirty(ent);
    }

    private void OnHookedStopped(Entity<ChampionHookedComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner != args.PulledUid)
            return;

        RemComp(ent, ent.Comp);
    }

    private void OnBeforeHarmfulAction(Entity<ChampionHookedComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Type == HarmfulActionType.Grab)
            args.Cancelled = true;
    }

    private void OnPullAttempt(Entity<ChampionHookedComponent> ent, ref BeingPulledAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnVirtualItems(Entity<ChampionHookComponent> ent, ref BeforeSpawnPullingVirtualItemsEvent args)
    {
        if (ent.Comp.HookedMob != args.Pulled)
            return;

        args.Cancelled = true;
    }

    private void OnAttack(Entity<ChampionHookComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (args.Type != ComboAttackType.Harm || !ent.Comp.IsEnabled ||
            !HasComp<HumanoidProfileComponent>(args.Target) || !HasComp<HereticBladeComponent>(args.Weapon) ||
            ent.Comp.Action is not { } action)
            return;

        _audio.PlayPredicted(ent.Comp.Sound, args.Target, ent);
        _action.SetToggled(action, false);
        _action.SetIfBiggerCooldown(action, ent.Comp.Cooldown);
        _dmg.ChangeDamage(args.Target, ent.Comp.ExtraDamage, origin: ent, canMiss: false);
        _pulling.StopAllPulls(ent, stopPullable: false);
        ent.Comp.IsEnabled = false;
        Dirty(ent);
        if (!_stun.TryKnockdown(args.Target, ent.Comp.KnockdownTime, autoStand: false))
            return;

        ent.Comp.HookedMob = args.Target;
        if (!_pulling.TryStartPull(ent,
                args.Target,
                grabStageOverride: GrabStage.Hard,
                escapeAttemptModifier: 0f,
                force: true))
        {
            ent.Comp.HookedMob = null;
            return;
        }

        ent.Comp.Weapon = args.Weapon;
        EnsureComp<ChampionHookedComponent>(args.Target);
    }

    private void OnHookToggle(Entity<ChampionHookComponent> ent, ref EventHereticToggleChampionHook args)
    {
        ent.Comp.IsEnabled = !ent.Comp.IsEnabled;
        ent.Comp.Action = args.Action;
        Dirty(ent);
        args.Toggle = true;
        args.Handled = true;
    }
}
