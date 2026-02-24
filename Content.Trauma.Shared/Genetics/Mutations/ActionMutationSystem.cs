// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed class ActionMutationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ActionMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<ActionMutationComponent> ent, ref MutationAddedEvent args)
    {
        // action shitcode spawns a clientside action
        if (_net.IsClient)
            return;

        _actions.AddAction(args.Target.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, container: ent.Owner);
        Dirty(ent);
    }

    private void OnRemoved(Entity<ActionMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (ent.Comp.ActionEntity is {} action)
            _actions.RemoveProvidedAction(args.Target.Owner, ent.Owner, action);
    }
}
