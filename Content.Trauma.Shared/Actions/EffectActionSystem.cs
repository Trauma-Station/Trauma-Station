// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Actions;

public sealed partial class EffectActionSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectActionComponent, ActionPerformedEvent>(OnActionPerformed);
        SubscribeLocalEvent<EffectActionComponent, EffectInstantActionEvent>(OnInstantAction);
        SubscribeLocalEvent<EffectActionComponent, EffectTargetActionEvent>(OnTargetAction);

        SubscribeLocalEvent<ToggleEffectActionComponent, EffectToggleActionEvent>(OnToggle);
    }

    private void OnActionPerformed(Entity<EffectActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (ent.Comp.OnPerformed)
            _effects.ApplyEffects(args.Performer, ent.Comp.Effects);
    }

    private void OnInstantAction(Entity<EffectActionComponent> ent, ref EffectInstantActionEvent args)
    {
        _effects.ApplyEffects(args.Performer, ent.Comp.Effects);
        args.Handled = true;
    }

    private void OnTargetAction(Entity<EffectActionComponent> ent, ref EffectTargetActionEvent args)
    {
        _effects.ApplyEffects(args.Target, ent.Comp.Effects);
        args.Handled = true;
    }

    private void OnToggle(Entity<ToggleEffectActionComponent> ent, ref EffectToggleActionEvent args)
    {
        bool targetState = !ent.Comp.Toggled;
        if (targetState && ent.Comp.OnToggleConditions is { } conditions)
        {
            if (!_conditions.TryConditions(args.Performer, conditions))
            {
                return;
            }
        }

        args.Handled = true;

        // If you modify args.Toggle directly and use it to check the conditions,
        // it will eventually lead to mispredicts (offEffects and onEffects getting applied constantly)
        // Conditions, on the other hand, don't need this.
        // So, storing a boolean on the component itself fixes those mispredicts.
        ent.Comp.Toggled = targetState;
        Dirty(ent);

        args.Toggle = targetState;

        if (ent.Comp.Toggled)
        {
            if (ent.Comp.OnToggle is not { } onEffects)
                return;

            _effects.ApplyEffects(args.Performer, onEffects);
            return;
        }

        if (ent.Comp.OffToggle is not { } offToggleEffects)
            return;

        _effects.ApplyEffects(args.Performer, offToggleEffects);
    }
}
