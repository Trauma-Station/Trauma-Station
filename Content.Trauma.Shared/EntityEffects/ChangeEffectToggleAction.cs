// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Actions;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that changes the toggle on an action with <see cref="ToggleEffectActionComponent"/>
/// </summary>
public sealed partial class ChangeEffectToggleAction : EntityEffectBase<ChangeEffectToggleAction>
{
    /// <summary>
    /// The action
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ToggleEffectActionComponent> Action;

    [DataField]
    public bool Toggle;
}

public sealed class ChangeEffectToggleActionEffectSystem : EntityEffectSystem<ActionsComponent, ChangeEffectToggleAction>
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<ActionsComponent> entity, ref EntityEffectEvent<ChangeEffectToggleAction> args)
    {
        var effect = args.Effect;
        if (!_actions.TryGetActionById(entity.Owner, effect.Action, out var toggleAction)
            || !TryComp<ToggleEffectActionComponent>(toggleAction, out var toggleActionComponent))
            return;

        toggleActionComponent.Toggled = args.Effect.Toggle;
        Dirty(entity);
    }
}
