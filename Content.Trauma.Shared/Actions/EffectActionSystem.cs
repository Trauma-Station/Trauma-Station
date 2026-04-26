// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Actions;

public sealed class EffectActionSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

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
        ent.Comp.Toggled = !ent.Comp.Toggled;
        args.Toggle = ent.Comp.Toggled;
        Dirty(ent);

        if (ent.Comp.Toggled && ent.Comp.OnToggle is { } onToggleEffects)
        {
            _effects.ApplyEffects(args.Performer, onToggleEffects);
            args.Handled = true;
            return;
        }

        if (ent.Comp.OffToggle is not { } offToggleEffects)
            return;

        _effects.ApplyEffects(args.Performer, offToggleEffects);
        args.Handled = true;
    }
}
