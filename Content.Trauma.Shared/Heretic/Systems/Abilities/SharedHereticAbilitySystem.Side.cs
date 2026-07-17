// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.DelayedKnockdown;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Ensnaring.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    private static readonly ProtoId<StatusEffectPrototype> Pacified = "Pacified";

    [SubscribeLocalEvent]
    private void OnCloak(EventHereticCloak args)
    {
        var ent = args.Performer;

        if (StatusNew.TryEffectsWithComp<HereticCloakedStatusEffectComponent>(ent, out var effects))
        {
            foreach (var effect in effects)
            {
                PredictedQueueDel(effect.Owner);
            }

            args.Handled = true;
            return;
        }

        // TryUseAbility only if we are not cloaked so that we can uncloak without focus
        if (!TryUseAbility(args))
            return;

        StatusNew.TryAddStatusEffect(ent, args.Status, out _, args.Lifetime);
    }

    [SubscribeLocalEvent]
    private void OnStatusEnded(Entity<RealignmentComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != Pacified)
            return;

        if (!StatusNew.TryRemoveStatusEffect(ent, ent.Comp.RealignmentStatus))
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnRealignment(EventHereticRealignment args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;

        foreach (var effect in args.RemovedEffects)
        {
            StatusNew.TryRemoveStatusEffect(ent, effect);
        }

        if (TryComp<StaminaComponent>(ent, out var stam))
        {
            if (stam.StaminaDamage >= stam.CritThreshold)
                _stam.ExitStamCrit(ent, stam);

            Dirty(ent, stam);
        }

        if (TryComp(ent, out CuffableComponent? cuffable) && _cuffs.TryGetLastCuff((ent, cuffable), out var cuffs))
            _cuffs.Uncuff(ent, null, cuffs.Value, cuffable);

        if (TryComp(ent, out EnsnareableComponent? ensnareable) && _snare.IsEnsnared((ent, ensnareable)))
        {
            var bola = ensnareable.Container!.ContainedEntities[0];
            _snare.ForceFree(bola);
        }

        _pulling.StopAllPulls(ent, stopPuller: false);

        RemComp<KnockedDownComponent>(ent);
        RemCompDeferred<DelayedKnockdownComponent>(ent);

        if (Status.TryAddStatusEffect<PacifiedComponent>(ent, "Pacified", args.EffectTime, true))
            StatusNew.TryUpdateStatusEffectDuration(ent, args.RealignmentStatus, out _, args.EffectTime);
    }

    [SubscribeLocalEvent]
    private void OnBeforeRealignmentStamina(Entity<RealignmentComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnEmp(EventEmp ev)
    {
        _emp.EmpPulse(Transform(ev.Performer).Coordinates, ev.Range, ev.EnergyConsumption, ev.Duration, ev.Performer);
        ev.Handled = true;
    }
}
