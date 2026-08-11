// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Implants;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System that allows the uplink implant to open the job board.
/// </summary>
public abstract partial class SharedJobListingsImplantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnImplantImplanted(Entity<JobListingsImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        ent.Comp.StoredAction = _actions.AddAction(args.Implanted, ent.Comp.Action, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnImplantRemoved(Entity<JobListingsImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (ent.Comp.StoredAction is not null)
            _actions.RemoveAction(ent.Comp.StoredAction);
    }
}


/// <summary>
/// Raised on the implant when the action to open the job board is used.
/// </summary>
public sealed partial class OpenJobListingsImplantEvent : InstantActionEvent;
