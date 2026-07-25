// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Commands;
using Content.Server.Database;
using Content.Server.Traitor.Uplink;
using Content.Shared.Actions;
using Content.Shared.Implants;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that allows the uplink implant to open the job board.
/// </summary>
public sealed partial class JobListingsSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    private void InitializeImplant()
    {
        SubscribeLocalEvent<RemoteJobListingsComponent, ImplantImplantedEvent>(OnImplantImplanted, after: [typeof(UplinkSystem)]);
        SubscribeLocalEvent<RemoteJobListingsComponent, ImplantRemovedEvent>(OnImplantRemoved);
        SubscribeLocalEvent<RemoteJobListingsComponent, OpenJobListingsImplantEvent>(OnImplantUsed);
    }

    private void OnImplantImplanted(Entity<RemoteJobListingsComponent> entity, ref ImplantImplantedEvent args)
    {
        var mind = _mind.GetMind(args.Implanted);
        if (mind is null)
            return;

        if (!TryComp<JobListingsOwnerComponent>(mind.Value, out var jobBoardOwner))
            return;
        entity.Comp.JobListings = jobBoardOwner.JobListings;

        entity.Comp.StoredActionOnImplant = _actions.AddAction(args.Implanted, entity.Comp.ActionOnImplant);
    }

    private void OnImplantRemoved(Entity<RemoteJobListingsComponent> entity, ref ImplantRemovedEvent args)
    {
        if (entity.Comp.StoredActionOnImplant is not null)
            _actions.RemoveAction(entity.Comp.StoredActionOnImplant);
    }

    private void OnImplantUsed(Entity<RemoteJobListingsComponent> entity, ref OpenJobListingsImplantEvent args)
    {
        OpenUi(entity.Owner, args.Performer);
    }
}


/// <summary>
/// Raised on the implant when the action to open the job board is used.
/// </summary>
public sealed partial class OpenJobListingsImplantEvent : InstantActionEvent;
