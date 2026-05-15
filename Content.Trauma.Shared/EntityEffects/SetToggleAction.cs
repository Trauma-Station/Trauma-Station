// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that changes the toggle on an action.
/// </summary>
public sealed partial class SetToggleAction : EntityEffectBase<SetToggleAction>
{
    /// <summary>
    /// Toggles or un-toggles an action
    /// </summary>
    [DataField]
    public bool Toggle;
}

public sealed class SetToggleActionEffectSystem : EntityEffectSystem<ActionComponent, SetToggleAction>
{
    [Dependency] private SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<ActionComponent> ent, ref EntityEffectEvent<SetToggleAction> args)
    {
        _actions.SetToggled(ent.AsNullable(), args.Effect.Toggle);
    }
}
