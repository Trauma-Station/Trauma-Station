// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that adds actions to the target entity.
/// </summary>
public sealed partial class AddActions : EntityEffectBase<AddActions>
{
    /// <summary>
    /// The actions to add.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ActionComponent>> Actions = new();
}

public sealed partial class AddActionsEffectSystem : EntityEffectSystem<ActionsComponent, AddActions>
{
    [Dependency] private SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<ActionsComponent> ent, ref EntityEffectEvent<AddActions> args)
    {
        foreach (var action in args.Effect.Actions)
        {
            _actions.AddAction(ent.Owner, action, component: ent.Comp);
        }
    }
}
