// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;

public sealed partial class HollowWeaveSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;

    [Dependency] private EntityQuery<RemoveOnAttackStatusEffectComponent> _removeQuery = default!;
    [Dependency] private EntityQuery<StatusEffectComponent> _statusQuery = default!;


    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<HollowWeaveComponent, BeforeDamageChangedEvent>(OnTakeDamage,
            baseEvent: false,
            held: false);
        Subs.SubscribeWithRelay<HollowWeaveComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction,
            baseEvent: false,
            held: false);

        SubscribeLocalEvent<StatusEffectContainerComponent, MeleeAttackEvent>(_status.RelayEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, AmmoShotUserEvent>(_status.RelayEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, ThrowEvent>(_status.RelayEvent);

        SubscribeLocalEvent<RemoveOnAttackStatusEffectComponent, StatusEffectRelayedEvent<MeleeAttackEvent>>(
            RemoveStatus);
        SubscribeLocalEvent<RemoveOnAttackStatusEffectComponent, StatusEffectRelayedEvent<AmmoShotUserEvent>>(
            RemoveStatus);
        SubscribeLocalEvent<RemoveOnAttackStatusEffectComponent, StatusEffectRelayedEvent<ThrowEvent>>(
            RemoveStatus);
    }

    private void RemoveStatus<T>(Entity<RemoveOnAttackStatusEffectComponent> ent, ref StatusEffectRelayedEvent<T> args)
    {
        var now = _timing.CurTime;
        if (args.Container.Comp.ActiveStatusEffects?.ContainedEntities.Where(
            x => _removeQuery.TryComp(x, out var comp) && now >= _statusQuery.Comp(x).StartEffectTime + comp.RemoveThreshold) is not { } effects)
            return;

        foreach (var effect in effects)
        {
            PredictedQueueDel(effect);
        }
    }

    private void OnTakeDamage(Entity<HollowWeaveComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.GetTotal() < 5)
            return;

        if (TryEvadeAttack(ent, args.Target, args.Origin))
            args.Cancelled = true;
    }

    private void OnBeforeHarmfulAction(Entity<HollowWeaveComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled || args.Type != HarmfulActionType.Harm)
            return;

        if (TryEvadeAttack(ent, args.Target, args.User))
            args.Cancelled = true;
    }

    private bool TryEvadeAttack(Entity<HollowWeaveComponent> ent, EntityUid target, EntityUid? user)
    {
        if (!_heretic.IsHereticOrGhoul(target))
            return false;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextStatus)
            return false;

        ent.Comp.NextStatus = now + ent.Comp.StatusDelay;
        Dirty(ent);

        _status.TryUpdateStatusEffectDuration(target, ent.Comp.StatusEffect, ent.Comp.StatusDuration);
        _audio.PlayPredicted(ent.Comp.Sound, target, user);
        return true;
    }
}
