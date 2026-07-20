// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public sealed partial class ChampionHookSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<MeleeWeaponComponent> _meleeQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChampionHookComponent, BeforeSpawnPullingVirtualItemsEvent>(OnVirtualItems);
        SubscribeLocalEvent<ChampionHookComponent, MeleeAttackEvent>(OnMelee);
        SubscribeLocalEvent<ChampionHookComponent, GetGrabMovespeedEvent>(OnGetMovespeed);
        SubscribeLocalEvent<ChampionHookComponent, PullStoppedMessage>(OnHookStopped);

        SubscribeLocalEvent<HereticBladeComponent, GotUnequippedHandEvent>(OnUnequipHand);
        SubscribeLocalEvent<HereticBladeComponent, AttemptMeleeEvent>(OnMeleeAttempt);

        SubscribeLocalEvent<ChampionHookedComponent, CanStandWhileImmobileEvent>(OnImmobileStand);
        SubscribeLocalEvent<ChampionHookedComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<ChampionHookedComponent, PullStoppedMessage>(OnHookedStopped);
        SubscribeLocalEvent<ChampionHookedComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<ChampionHookedComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction);

        SubscribeLocalEvent<CrawlerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<CrawlerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (user == ent.Owner || args.Using is not { } used)
            return;

        if (!CanHook(user, used, out _))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 9,
            Act = () =>
            {
                if (!CanHook(user, used, out var hook)) // check again
                    return;

                DoHook((user, hook), used, ent);
            },
        });
    }

    private bool CanHook(EntityUid user, EntityUid used, [NotNullWhen(true)] out ChampionHookComponent? hook)
    {
        return TryComp(user, out hook) && hook.HookedMob == null && _combat.IsInCombatMode(user) &&
               HasComp<HereticBladeComponent>(used);
    }

    private void OnUnequipHand(Entity<HereticBladeComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!TryComp(args.User, out ChampionHookComponent? hook) || hook.Weapon != ent.Owner ||
            hook.HookedMob is not { } hooked || !TryComp(hooked, out PullableComponent? pullable))
            return;

        _pulling.TryStopPull(hooked, pullable, args.User, true);
    }

    private void OnMeleeAttempt(Entity<HereticBladeComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!TryComp(args.User, out ChampionHookComponent? hook) || hook.Weapon != ent.Owner ||
            hook.HookedMob == null)
            return;

        args.Cancelled = true;

        args.WeaponComponent.NextAttack = TimeSpan.Zero;
        Dirty(args.Weapon, args.WeaponComponent);

        // Attempt attacking with offhand weapon
        foreach (var held in _hands.EnumerateHeld(args.User))
        {
            if (held == ent.Owner || !_meleeQuery.TryComp(held, out var melee))
                continue;

            AttackEvent ev;
            switch (args.attack)
            {
                case LightAttackEvent light:
                    ev = new LightAttackEvent(light.Target, GetNetEntity(held), light.Coordinates, light.IsLeftClick);
                    break;
                case HeavyAttackEvent heavy:
                    ev = new HeavyAttackEvent(GetNetEntity(held), heavy.Entities, heavy.Coordinates);
                    break;
                default:
                    return;
            }

            if (_melee.AttemptAttack(args.User, held, melee, ev, CompOrNull<ActorComponent>(args.User)?.PlayerSession))
                return;
        }
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

    private void DoHook(Entity<ChampionHookComponent> ent, EntityUid used, EntityUid target)
    {
        if (!TryComp(used, out MeleeWeaponComponent? melee))
            return;
        if (!_melee.AttemptLightAttack(ent, used, melee, target, false))
            return;

        melee.NextAttack += TimeSpan.FromSeconds(1f / _melee.GetAttackRate(used, ent, melee));
        Dirty(used, melee);

        _audio.PlayPredicted(ent.Comp.Sound, target, ent);
        _dmg.ChangeDamage(target, ent.Comp.ExtraDamage, origin: ent, canMiss: false);
        _pulling.StopAllPulls(ent, stopPullable: false);
        Dirty(ent);
        if (!_stun.TryKnockdown(target, ent.Comp.KnockdownTime, autoStand: false))
            return;

        ent.Comp.HookedMob = target;
        if (!_pulling.TryStartPull(ent,
                target,
                grabStageOverride: GrabStage.Soft,
                escapeAttemptModifier: 0f,
                force: true))
        {
            ent.Comp.HookedMob = null;
            return;
        }

        ent.Comp.Weapon = used;
        EnsureComp<ChampionHookedComponent>(target);
    }
}
