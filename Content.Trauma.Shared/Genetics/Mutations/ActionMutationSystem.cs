// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed class ActionMutationSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionMutationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ActionMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ActionMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnMapInit(Entity<ActionMutationComponent> ent, ref MapInitEvent args)
    {
        var container = EnsureComp<ActionsContainerComponent>(ent);
        _actionContainer.AddAction(ent, ent.Comp.Action, container);
    }

    private void OnAdded(Entity<ActionMutationComponent> ent, ref MutationAddedEvent args)
    {
        _actions.GrantContainedActions(args.Target.Owner, ent.Owner);
    }

    private void OnRemoved(Entity<ActionMutationComponent> ent, ref MutationRemovedEvent args)
    {
        _actions.RemoveProvidedActions(args.Target.Owner, ent.Owner);
    }
}
