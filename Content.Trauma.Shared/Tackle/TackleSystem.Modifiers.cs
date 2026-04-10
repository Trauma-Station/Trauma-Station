// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.Mutate;
using Content.Shared.Clumsy;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.Tackle;

public sealed partial class TackleSystem
{
    private void InitializeModifiers()
    {
        SubscribeLocalEvent<HulkComponent, CalculateTackleModifierEvent>(OnHulk);
        SubscribeLocalEvent<ClumsyComponent, CalculateTackleModifierEvent>(OnClumsy);
        SubscribeLocalEvent<PhysicsComponent, CalculateTackleModifierEvent>(OnMass);
        SubscribeLocalEvent<StaminaComponent, CalculateTackleModifierEvent>(OnStamina);
        SubscribeLocalEvent<MobThresholdsComponent, CalculateTackleModifierEvent>(OnThresholds);
    }

    private void OnThresholds(Entity<MobThresholdsComponent> ent, ref CalculateTackleModifierEvent args)
    {
        if (!TryComp(ent, out DamageableComponent? damageable))
            return;

        var total = _dmg.GetTotalDamage((ent.Owner, damageable));
        if (_threshold.TryGetThresholdForState(ent, MobState.SoftCrit, out var threshold) ||
            _threshold.TryGetThresholdForState(ent, MobState.Critical, out threshold) && threshold > 0f)
            args.Modifier -= (total * threshold.Value / 2).Float();
    }

    private void OnStamina(Entity<StaminaComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier -= ent.Comp.StaminaDamage / ent.Comp.CritThreshold * 2f;
    }

    private void OnMass(Entity<PhysicsComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier += (ent.Comp.Mass / 140f - 0.5f) * 2f;
    }

    private void OnClumsy(Entity<ClumsyComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier -= 2f;
    }

    private void OnHulk(Entity<HulkComponent> ent, ref CalculateTackleModifierEvent args)
    {
        args.Modifier += 2f;
    }
}
