// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Mind;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Store.Components;
using Content.Trauma.Shared.JobListings;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the side-jobs for progressive traitor.
/// </summary>

public sealed partial class JobListingsSystem : SharedJobListingsSystem
{
    [Dependency] private ObjectivesSystem _objectives = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialised);
        SubscribeLocalEvent<JobListingsComponent, MapInitEvent>(OnInit);
    }

    public override void OpenUi(Entity<JobListingsComponent> ent)
    {
        UpdateUi(ent);
    }

    public void UpdateUi(Entity<JobListingsComponent> ent)
    {

    }

    /// <summary>
    /// Assign the store owner a random side job.
    /// When the traitor is assigned their uplink, the traitor's mind becomes the store's owner.
    /// </summary>
    /// <param name="jobBoard">The entity of the store and job board.</param>
    /// <returns>True if successful, false if failure.</returns>
    public bool AssignSideJob(Entity<StoreComponent, JobListingsComponent> jobBoard)
    {
        if (jobBoard.Comp1.AccountOwner is null)
            return false;

        var mind = jobBoard.Comp1.AccountOwner.Value;
        if (!TryComp<MindComponent>(mind, out var mindComp))
            return false;

        var possibleJobs = jobBoard.Comp2.SideJobs.ShallowClone();

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(jobBoard.Owner));
        while (possibleJobs.Count > 0)
        {
            var index = random.Next(possibleJobs.Count);
            var job = possibleJobs[index];
            possibleJobs.RemoveAt(index);

            if (!_objectives.TryCreateObjective((mind, mindComp), job, out var sideJob))
                return false;

            _container.Insert(sideJob.Value, jobBoard.Comp2.AvailableSideJobsContainer);
        }

        return false;
    }

    /// <summary>
    /// Count how many jobs exist on the job board.
    /// This includes both available and assigned.
    /// </summary>
    /// <param name="jobBoard"></param>
    /// <returns>True if successful, false if failure.</returns>
    public int CountSideJobs(Entity<StoreComponent, JobListingsComponent> jobBoard)
    {
        return jobBoard.Comp2.AvailableSideJobsContainer.Count;
    }

    /// <summary>
    /// Assign the traitor side jobs until their available slots are filled.
    /// </summary>
    /// <param name="jobBoard"></param>
    /// <returns>True if successful, false if failure.</returns>
    public bool FillSideJobs(Entity<StoreComponent, JobListingsComponent> jobBoard)
    {
        while (CountSideJobs(jobBoard) < jobBoard.Comp2.JobCount)
        {
            if (!AssignSideJob(jobBoard))
                return false;
        }

        return true;
    }

    private void OnInit(Entity<JobListingsComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.AvailableSideJobsContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.AvailableSideJobsContainerId);
        if(!_ui.HasUi(ent.Owner, JobListingsUiKey.Key))
            _ui.SetUi(ent.Owner, JobListingsUiKey.Key, new InterfaceData("JobListingsBoundUserInterface"));
    }

    private void OnStoreInitialised(ref StoreInitializedEvent args)
    {
        if (!TryComp<StoreComponent>(args.Store, out var storeComp))
            return;
        if (!TryComp<JobListingsComponent>(args.Store, out var jobListingsComp))
            return;

        FillSideJobs((args.Store, storeComp, jobListingsComp));
    }
}
