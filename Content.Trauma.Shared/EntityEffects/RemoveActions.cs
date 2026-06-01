// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that removes actions from a target.
/// </summary>
public sealed partial class RemoveActions : EntityEffectBase<RemoveActions>
{
    /// <summary>
    /// The actions to remove.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ActionComponent>> Actions = new();
}

public sealed partial class ReplaceActionEffectSystem : EntityEffectSystem<ActionsComponent, RemoveActions>
{
    [Dependency] private SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<ActionsComponent> ent, ref EntityEffectEvent<RemoveActions> args)
    {
        var user = ent.Owner;
        foreach (var action in args.Effect.Actions)
        {
            if (!_actions.TryGetActionById(user, action, out var actionComp)
                || actionComp is not { } actionToRemove)
                continue;

            _actions.RemoveAction(ent.AsNullable(), actionToRemove.AsNullable());
        }
    }
}
