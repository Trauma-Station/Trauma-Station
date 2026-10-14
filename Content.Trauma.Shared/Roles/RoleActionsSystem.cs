// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Trauma.Common.Roles;

namespace Content.Trauma.Shared.Roles;

public sealed partial class RoleActionsSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnAdded(Entity<RoleActionsComponent> ent, ref RoleGotAddedEvent args)
    {
        foreach (var id in ent.Comp.Actions)
        {
            if (_actionContainer.AddAction(args.Mind, id) is { } action)
                ent.Comp.ActionEntities.Add(action);
        }
        Dirty(ent);

        if (args.Mob is { } mob)
            _actions.GrantActions(mob, ent.Comp.ActionEntities, args.Mind);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<RoleActionsComponent> ent, ref RoleGotRemovedEvent args)
    {
        if (args.Mob is { } mob)
            RemoveActions(ent, mob, args.Mind);
    }

    [SubscribeLocalEvent]
    private void OnMindAdded(Entity<RoleActionsComponent> ent, ref RoleMindAddedEvent args)
    {
        _actions.GrantActions(args.Mob, ent.Comp.ActionEntities, args.Mind);
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(Entity<RoleActionsComponent> ent, ref RoleMindRemovedEvent args)
    {
        RemoveActions(ent, args.Mob, args.Mind);
    }

    private void RemoveActions(Entity<RoleActionsComponent> ent, EntityUid mob, EntityUid mind)
    {
        foreach (var action in ent.Comp.ActionEntities)
        {
            _actions.RemoveProvidedAction(mob, mind, action);
        }
    }
}
