// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Events;

namespace Content.Trauma.Shared.Actions;

public sealed class RemoveActionOnPerformSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoveActionOnPerformComponent, ActionPerformedEvent>(OnPerform);
    }

    private void OnPerform(Entity<RemoveActionOnPerformComponent> ent, ref ActionPerformedEvent args)
    {
        _actions.RemoveAction(args.Performer, ent.Owner);
    }
}
