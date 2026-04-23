// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.StatusEffects;

public sealed class DamageModifyStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, DamageModifyEvent>(_statusEffects.RelayStatusEffectEvent);

        SubscribeLocalEvent<DamageModifyStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(Entity<DamageModifyStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        var ev = args.Args;
        ev.Damage = DamageSpecifier.ApplyModifierSet(ev.Damage, ent.Comp.Modifiers);
        args.Args = ev;
    }
}
