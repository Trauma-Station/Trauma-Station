// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Weapons.Ranged;
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

public sealed class HollowWeaveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly EntityQuery<RemoveOnAttackStatusEffectComponent> _removeQuery = default!;


    public override void Initialize()
    {
        base.Initialize();

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
        if (args.Container.Comp.ActiveStatusEffects?.ContainedEntities.Where(_removeQuery.HasComp) is not { } effects)
            return;

        foreach (var effect in effects)
        {
            PredictedQueueDel(effect);
        }
    }

    private void OnBeforeHarmfulAction(Entity<HollowWeaveComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Type != HarmfulActionType.Harm || args.Target == args.User)
            return;

        if (!_heretic.IsHereticOrGhoul(args.Target))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextStatus)
            return;

        ent.Comp.NextStatus = now + ent.Comp.StatusDelay;
        Dirty(ent);

        args.Cancelled = true;

        _status.TryUpdateStatusEffectDuration(args.Target, ent.Comp.StatusEffect, ent.Comp.StatusDuration);
        _audio.PlayPredicted(ent.Comp.Sound, args.Target, args.User);
    }
}
