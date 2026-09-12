// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.Mutate;
using Content.Shared.Clumsy;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.Tackle;

public sealed partial class TackleSystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    private void InitializeModifiers()
    {
        SubscribeLocalEvent<StatusEffectContainerComponent, CalculateTackleModifierEvent>(_status.RelayEvent);
    }

    [SubscribeLocalEvent]
    private void OnThresholds(Entity<MobThresholdsComponent> ent, ref CalculateTackleModifierEvent args)
    {
        if (!TryComp(ent, out DamageableComponent? damageable))
            return;

        var total = _dmg.GetTotalDamage((ent.Owner, damageable));
        if (_threshold.TryGetThresholdForState(ent, MobState.SoftCrit, out var threshold) ||
            _threshold.TryGetThresholdForState(ent, MobState.Critical, out threshold) && threshold > 0f)
            args.Modifier -= (total / threshold.Value / 2).Float();
    }

    [SubscribeLocalEvent]
    private void OnStamina(Entity<StaminaComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier -= ent.Comp.StaminaDamage / ent.Comp.CritThreshold;
    }

    [SubscribeLocalEvent]
    private void OnMass(Entity<PhysicsComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier += (ent.Comp.Mass / 140f - 0.5f) * 2f;
    }

    [SubscribeLocalEvent]
    private void OnStatusEffect(Entity<TackleModStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CalculateTackleModifierEvent> args)
    {
        // an IQ too high?
        var ev = args.Args;
        ev.Modifier += ent.Comp.Modifier;
        args.Args = ev;
    }

    [SubscribeLocalEvent]
    private void OnHulk(Entity<HulkComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier += 2f;
    }
}
