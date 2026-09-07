// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Server.GameStates;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Traitor;
using Content.Trauma.Shared.JobListings;
using System.Linq;

namespace Content.Trauma.Server.JobListings;

public sealed partial class JobListingsSystem : SharedJobListingsSystem
{
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Assign the store owner a random side job.
    /// When the traitor is assigned their uplink, the traitor's mind becomes the store's owner.
    /// This has to be server-side because too many objective event handlers are server-side.
    /// </summary>
    public bool AssignSideJob(Entity<JobListingsComponent> jobBoard, int effectiveLevel)
    {
        if (jobBoard.Comp.Mind is null)
            return false;

        var mind = GetEntity(jobBoard.Comp.Mind.Value);
        if (!MindQuery.TryComp(mind, out var mindComp))
            return false;
        var actor = mindComp.OwnedEntity;
        if (actor is null)
            return false;

        var possibleJobs = jobBoard.Comp.SideJobOffers.ShallowClone();
        var possiblePriorityJobs = jobBoard.Comp.PrioritySideJobOffers.ShallowClone();

        while (possiblePriorityJobs.Count > 0 || possibleJobs.Count > 0)
        {
            var shouldChoosePriority = possiblePriorityJobs.Count > 0;
            var job = _random.PickAndTake(shouldChoosePriority ? possiblePriorityJobs : possibleJobs);

            if (!CanAddSideJob(jobBoard, job))
                continue;

            // spawn the objective in directly, ignoring RequirementCheckEvent
            // (otherwise it would do things like cancel steal sidejobs for DAGD traitors or kill sidejobs for social traitors)
            var sideJob = Spawn(job);
            if (!ObjectiveQuery.TryComp(sideJob, out var objectiveComp))
            {
                Del(sideJob);
                continue;
            }

            // raise events to initialise the objectives
            var ev1 = new ObjectiveAssignedEvent(mind, mindComp);
            RaiseLocalEvent(sideJob, ref ev1);
            if (ev1.Cancelled)
            {
                Del(sideJob);
                continue;
            }

            var ev3 = new SideJobCreatedEvent(effectiveLevel);
            RaiseLocalEvent(sideJob, ref ev3);

            // if initialising failed then abort
            if (ev3.Cancelled || !SideJobQuery.TryComp(sideJob, out var sideJobComp) || sideJobComp.Reward is null)
            {
                Del(sideJob);
                continue;
            }

            var ev2 = new ObjectiveAfterAssignEvent(mind, mindComp, objectiveComp, MetaData(sideJob));
            RaiseLocalEvent(sideJob, ref ev2);

            Container.Insert(sideJob, jobBoard.Comp.AvailableSideJobs);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if the job board already has the current side job as either available, accepted or completed.
    /// Used to avoid adding the same objective twice.
    /// </summary>
    public bool CanAddSideJob(Entity<JobListingsComponent> jobBoard, EntProtoId sideJobProtoId)
    {
        if (jobBoard.Comp.CompletedObjectives.Contains(sideJobProtoId))
            return false;

        foreach (var sideJob in jobBoard.Comp.AvailableSideJobs.ContainedEntities)
        {
            var availableSideJobProto = Prototype(sideJob);
            if (availableSideJobProto is not null && availableSideJobProto.ID == sideJobProtoId)
                return false;
        }
        foreach (var sideJob in jobBoard.Comp.AcceptedSideJobs.ContainedEntities)
        {
            var acceptedSideJobProto = Prototype(sideJob);
            if (acceptedSideJobProto is not null && acceptedSideJobProto.ID == sideJobProtoId)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Assign the traitor side jobs until their available slots are filled.
    /// </summary>
    public bool FillSideJobs(Entity<JobListingsComponent> jobBoard)
    {
        var effectiveLevel = GetReputationLevel(jobBoard);
        var jobsAssignedOfCurrentLevel = 0;

        while (CountSideJobs(jobBoard) < jobBoard.Comp.MaximumSideJobs)
        {
            if (!AssignSideJob(jobBoard, effectiveLevel))
            {
                // if we are above 0 effective level, try reduce it by 1 and try again to assign
                if (effectiveLevel > 0)
                {
                    jobsAssignedOfCurrentLevel = 0;
                    effectiveLevel -= 1;
                    continue;
                }

                return false;
            }

            // if we reached the limit then start assigning jobs of the level below
            // this is so that when we reach a new level we still get some jobs of the old level to keep things interesting
            jobsAssignedOfCurrentLevel += 1;
            if (jobsAssignedOfCurrentLevel >= jobBoard.Comp.SideJobsPerLevel && effectiveLevel > 0)
            {
                jobsAssignedOfCurrentLevel = 0;
                effectiveLevel -= 1;
            }
        }

        return true;
    }

    public override void Refresh(Entity<JobListingsComponent> jobBoard)
    {
        base.Refresh(jobBoard);
        // make copy so you dont mutate while iterating
        var copy = jobBoard.Comp.AcceptedSideJobs.ContainedEntities.ToList();
        foreach (var sideJob in copy)
        {
            Del(sideJob);
        }
        FillSideJobs(jobBoard);
    }

    /// <summary>
    /// Helper method to add a PVS override for the job board and sidejobs.
    /// They are nullspace entities on the server and would not normally be replicated to the client but this method makes it so.
    /// </summary>
    private void PVSOverrideEntity(Entity<MindComponent> mind, EntityUid entity)
    {
        if (mind.Comp.OwnedEntity is null)
            return;
        if (!_player.TryGetSessionByEntity(mind.Comp.OwnedEntity.Value, out var session))
            return;
        _pvsOverride.AddSessionOverride(entity, session);
    }

    /// <summary>
    /// Update the CachedProgress field on a sidejob.
    /// </summary>
    /// <param name="sideJob"></param>
    private void UpdateSideJob(Entity<JobListingsComponent> jobBoard, Entity<SideJobComponent> sideJob)
    {
        if (jobBoard.Comp.Mind is null)
            return;
        var mind = GetEntity(jobBoard.Comp.Mind);
        if (!MindQuery.TryComp(mind, out var mindComp))
            return;
        var progress = _objectives.GetProgress(sideJob.Owner, (mind.Value, mindComp));
        if (progress is null)
            return;
        sideJob.Comp.CachedProgress = progress.Value;
        DirtyField(sideJob.Owner, sideJob.Comp, nameof(SideJobComponent.CachedProgress));
    }

    protected override void UpdateAllSideJobs(Entity<JobListingsComponent> jobBoard)
    {
        foreach (var sideJob in jobBoard.Comp.AvailableSideJobs.ContainedEntities)
        {
            if (!SideJobQuery.TryComp(sideJob, out var sideJobComp))
                return;
            UpdateSideJob(jobBoard, (sideJob, sideJobComp));
        }

        foreach (var sideJob in jobBoard.Comp.AcceptedSideJobs.ContainedEntities)
        {
            if (!SideJobQuery.TryComp(sideJob, out var sideJobComp))
                return;
            UpdateSideJob(jobBoard, (sideJob, sideJobComp));
        }
    }

    [SubscribeLocalEvent]
    private void OnUplinkAssigned(ref UplinkAssignedEvent args)
    {
        if (!JobListingsQuery.TryComp(args.Uplink, out var jobListingsComp))
            return;
        if (Mind.GetMind(args.User) is not { } mind)
            return;
        if (!MindQuery.TryComp(mind, out var mindComp))
            return;

        // set mind
        jobListingsComp.Mind = GetNetEntity(mind);
        DirtyField(args.Uplink, jobListingsComp, nameof(JobListingsComponent.Mind));
        PVSOverrideEntity((mind, mindComp), args.Uplink);
        AddComp(mind, new JobListingsOwnerComponent { JobListings = GetNetEntity(args.Uplink) });

        // init job board
        FillSideJobs((args.Uplink, jobListingsComp));
        Link((args.Uplink, jobListingsComp), args.Host);
        SetRefreshTime((args.Uplink, jobListingsComp));
    }

    [SubscribeLocalEvent]
    private void OnUplinkLinked(ref UplinkLinkedEvent args)
    {
        if (!JobListingsQuery.TryComp(args.Uplink, out var jobListingsComp))
            return;

        Link((args.Uplink, jobListingsComp), args.Host);
    }
}
