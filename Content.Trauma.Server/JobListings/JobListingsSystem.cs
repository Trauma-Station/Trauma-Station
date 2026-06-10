// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives;
using Content.Server.PDA;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Common.Traitor;
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
    [Dependency] private PdaSystem _pda = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UplinkAssignedEvent>(OnUplinkAssigned);
        SubscribeLocalEvent<PdaComponent, PdaShowJobListingsMessage>(OnMessage);
    }

    /// <summary>
    /// Assign the store owner a random side job.
    /// When the traitor is assigned their uplink, the traitor's mind becomes the store's owner.
    /// </summary>
    /// <param name="jobBoard">The entity of the store and job board.</param>
    /// <returns>True if successful, false if failure.</returns>
    public bool AssignSideJob(Entity<JobListingsComponent> jobBoard)
    {
        if (jobBoard.Comp.Mind is null)
            return false;

        var mind = jobBoard.Comp.Mind.Value;
        if (!TryComp<MindComponent>(mind, out var mindComp))
            return false;

        var possibleJobs = jobBoard.Comp.SideJobs.ShallowClone();

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(jobBoard.Owner));
        while (possibleJobs.Count > 0)
        {
            var index = random.Next(possibleJobs.Count);
            var job = possibleJobs[index];
            possibleJobs.RemoveAt(index);

            if (!_objectives.TryCreateObjective((mind, mindComp), job, out var sideJob))
                return false;

            jobBoard.Comp.AvailableSideJobs.Add(sideJob.Value);
        }

        return false;
    }

    /// <summary>
    /// Count how many jobs exist on the job board.
    /// This includes both available and assigned.
    /// </summary>
    /// <param name="jobBoard"></param>
    /// <returns>True if successful, false if failure.</returns>
    public int CountSideJobs(Entity<JobListingsComponent> jobBoard)
    {
        return jobBoard.Comp.AvailableSideJobs.Count;
    }

    /// <summary>
    /// Assign the traitor side jobs until their available slots are filled.
    /// </summary>
    /// <param name="jobBoard"></param>
    /// <returns>True if successful, false if failure.</returns>
    public bool FillSideJobs(Entity<JobListingsComponent> jobBoard)
    {
        while (CountSideJobs(jobBoard) < jobBoard.Comp.JobCount)
        {
            if (!AssignSideJob(jobBoard))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Open the job listings ui.
    /// </summary>
    /// <param name="owner">The entity which owns the Ui, probably a PDA.</param>
    /// <param name="actor">The player opening the Ui.</param>
    public void OpenUi(EntityUid owner, EntityUid actor)
    {
        _ui.TryOpenUi(owner, JobListingsUiKey.Key, actor);
        UpdateUi(owner);
    }

    public void UpdateUi(EntityUid owner)
    {

    }

    private void OnUplinkAssigned(ref UplinkAssignedEvent args)
    {
        if (!TryComp<JobListingsComponent>(args.Store, out var jobListingsComp))
            return;

        jobListingsComp.Mind = args.Mind;
        FillSideJobs((args.Store, jobListingsComp));

        AddComp(args.Host, new RemoteJobListingsComponent {JobListings = args.Store});
    }

    private void OnMessage(Entity<PdaComponent> pda, ref PdaShowJobListingsMessage msg)
    {
        OpenUi(pda, msg.Actor);
    }
}
