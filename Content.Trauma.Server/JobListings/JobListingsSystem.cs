// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Mind;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Store.Components;
using Content.Trauma.Common.Store;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the side-jobs for progressive traitor.
/// </summary>

public sealed partial class JobListingsSystem : EntitySystem
{
    [Dependency] private ObjectivesSystem _objectives = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialised);
    }

    /// <summary>
    /// Assign the store owner a random side job.
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

            if (_objectives.TryCreateObjective((mind, mindComp), job, out _))
                return true;
        }

        return false;
    }

    private void OnStoreInitialised(ref StoreInitializedEvent args)
    {
        if (!TryComp<StoreComponent>(args.Store, out var storeComp))
            return;
        if (!TryComp<JobListingsComponent>(args.Store, out var jobListingsComp))
            return;

        for (var i = 0; i < jobListingsComp.JobCount; i++)
        {
            AssignSideJob((args.Store, storeComp, jobListingsComp));
        }
    }
}
