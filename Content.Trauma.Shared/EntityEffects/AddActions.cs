// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds actions to the target entity
/// </summary>
public sealed partial class AddActions : EntityEffectBase<AddActions>
{
    /// <summary>
    /// The actions to add
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ActionComponent>> Actions = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class AddActionsEffectSystem : EntityEffectSystem<ActionsComponent, AddActions>
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<ActionsComponent> ent, ref EntityEffectEvent<AddActions> args)
    {
        foreach (var action in args.Effect.Actions)
        {
            _actions.AddAction(ent.Owner, action, component: ent.Comp);
        }
    }
}
