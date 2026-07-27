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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JobListingsImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<JobListingsImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    private void OnImplantImplanted(Entity<JobListingsImplantComponent> entity, ref ImplantImplantedEvent args)
    {
        entity.Comp.StoredAction = _actions.AddAction(args.Implanted, entity.Comp.Action, entity.Owner);
    }

    private void OnImplantRemoved(Entity<JobListingsImplantComponent> entity, ref ImplantRemovedEvent args)
    {
        if (entity.Comp.StoredAction is not null)
            _actions.RemoveAction(entity.Comp.StoredAction);
    }
}


/// <summary>
/// Raised on the implant when the action to open the job board is used.
/// </summary>
public sealed partial class OpenJobListingsImplantEvent : InstantActionEvent;
