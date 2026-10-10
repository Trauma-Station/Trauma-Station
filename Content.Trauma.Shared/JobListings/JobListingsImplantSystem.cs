// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System that allows the uplink implant to open the job board.
/// </summary>
public sealed partial class JobListingsImplantSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private JobListingsSystem _jobs = default!;

    private void AddAction(Entity<JobListingsImplantComponent> ent)
    {
        if (!TryComp<SubdermalImplantComponent>(ent, out var implantComp) || implantComp.ImplantedEntity is not { } implanted)
            return;
        ent.Comp.StoredAction = _actions.AddAction(implanted, ent.Comp.Action, ent);
        Dirty(ent);
    }

    private void RemoveAction(Entity<JobListingsImplantComponent> ent)
    {
        if (ent.Comp.StoredAction is null)
            return;

        _actions.RemoveAction(ent.Comp.StoredAction);
        ent.Comp.StoredAction = null;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnJobListingsVisibilityUpdated(Entity<JobListingsImplantComponent> ent, ref JobListingsVisibilityUpdatedEvent args)
    {
        if (!TryComp<RemoteJobListingsComponent>(ent, out var remoteComp))
            return;

        if (_jobs.IsJobBoardHidden((ent, remoteComp)))
            RemoveAction(ent);
        else
            AddAction(ent);
    }

    [SubscribeLocalEvent]
    private void OnImplantRemoved(Entity<JobListingsImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemoveAction(ent);
    }

    [SubscribeLocalEvent]
    private void OnImplantUsed(Entity<JobListingsImplantComponent> ent, ref OpenJobListingsImplantEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        _jobs.OpenUi(ent, user);
        args.Handled = true;
    }
}

/// <summary>
/// Raised on the implant when the action to open the job board is used.
/// </summary>
[DataDefinition]
public sealed partial class OpenJobListingsImplantEvent : InstantActionEvent;
