using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Common.Traitor;
using Content.Trauma.Server.JobListings;
using Content.Trauma.Shared.Heretic.Rituals;
using Content.Trauma.Shared.JobListings;
using Robust.Server.GameStates;
using Robust.Shared.Player;

public sealed partial class JobListingsSystem : SharedJobListingsSystem
{
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    /// <summary>
    /// Assign the store owner a random side job.
    /// When the traitor is assigned their uplink, the traitor's mind becomes the store's owner.
    /// </summary>
    public bool AssignSideJob(Entity<JobListingsComponent> jobBoard, int effectiveLevel)
    {
        if (jobBoard.Comp.Mind is null)
            return false;

        var mind = GetEntity(jobBoard.Comp.Mind.Value);
        if (!TryComp<MindComponent>(mind, out var mindComp))
            return false;

        var possibleJobs = jobBoard.Comp.SideJobOffers.ShallowClone();
        var possiblePriorityJobs = jobBoard.Comp.PrioritySideJobOffers.ShallowClone();
        var random = SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(jobBoard.Owner));

        while (possiblePriorityJobs.Count > 0 || possibleJobs.Count > 0)
        {
            EntProtoId job;
            if (possiblePriorityJobs.Count > 0)
            {
                var index = random.Next(possiblePriorityJobs.Count);
                job = possiblePriorityJobs[index];
                possiblePriorityJobs.RemoveAt(index);
            }
            else
            {
                var index = random.Next(possibleJobs.Count);
                job = possibleJobs[index];
                possibleJobs.RemoveAt(index);
            }

            if (!CanAddSideJob(jobBoard, job))
                continue;

            // spawn the objective in directly, ignoring RequirementCheckEvent
            // (otherwise it would do things like cancel steal sidejobs for DAGD traitors or kill sidejobs for social traitors)
            var sideJob = Spawn(job);
            if (!TryComp<ObjectiveComponent>(sideJob, out var objectiveComp))
            {
                QueueDel(sideJob);
                continue;
            }

            // raise events to initialise the objectives
            var ev1 = new ObjectiveAssignedEvent(mind, mindComp);
            RaiseLocalEvent(sideJob, ref ev1);
            var ev2 = new ObjectiveAfterAssignEvent(mind, mindComp, objectiveComp, MetaData(sideJob));
            RaiseLocalEvent(sideJob, ref ev2);
            var ev3 = new SideJobCreatedEvent(effectiveLevel);
            RaiseLocalEvent(sideJob, ref ev3);

            // if initialising failed then abort
            if (ev1.Cancelled || ev3.Cancelled || !TryComp<SideJobComponent>(sideJob, out var sideJobComp) || sideJobComp.Reward is null)
            {
                QueueDel(sideJob);
                continue;
            }

            jobBoard.Comp.AvailableSideJobs.Add(GetNetEntity(sideJob));
            DirtyField(jobBoard.AsNullable(), nameof(JobListingsComponent.AvailableSideJobs));
            PVSOverrideEntity((mind, mindComp), sideJob);
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

        foreach (var availableSideJob in jobBoard.Comp.AvailableSideJobs)
        {
            var availableSideJobProto = MetaData(GetEntity(availableSideJob)).EntityPrototype;
            if (availableSideJobProto is not null && availableSideJobProto.ID == sideJobProtoId)
                return false;
        }
        foreach (var acceptedSideJob in jobBoard.Comp.AcceptedSideJobs)
        {
            var acceptedSideJobProto = MetaData(GetEntity(acceptedSideJob)).EntityPrototype;
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

    /// <summary>
    /// Refresh the job board.
    /// This has no checks and should only be called if <see cref="CanRefresh"/> returns true.
    /// This method deletes every job not currently accepted and then assigns jobs until the job board is full.
    /// </summary>
    public void Refresh(Entity<JobListingsComponent> jobBoard)
    {
        foreach (var job in jobBoard.Comp.AvailableSideJobs)
        {
            QueueDel(GetEntity(job));
        }

        jobBoard.Comp.AvailableSideJobs.Clear();
        jobBoard.Comp.BonusRefresh = false;
        DirtyFields(jobBoard.AsNullable(), null, nameof(JobListingsComponent.AvailableSideJobs), nameof(JobListingsComponent.BonusRefresh));

        SetRefreshTime(jobBoard);
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

    [SubscribeLocalEvent]
    private void OnUplinkAssigned(ref UplinkAssignedEvent args)
    {
        if (!TryComp<JobListingsComponent>(args.Uplink, out var jobListingsComp))
            return;
        var mind = Mind.GetMind(args.User);
        if (mind is null)
            return;
        if (!TryComp<MindComponent>(mind, out var mindComp))
            return;

        // set mind
        jobListingsComp.Mind = GetNetEntity(mind.Value);
        DirtyField(args.Uplink, jobListingsComp, nameof(JobListingsComponent.Mind));
        PVSOverrideEntity((mind.Value, mindComp), args.Uplink);
        AddComp(mind.Value, new JobListingsOwnerComponent { JobListings = GetNetEntity(args.Uplink) });

        // init job board
        FillSideJobs((args.Uplink, jobListingsComp));
        Link((args.Uplink, jobListingsComp), args.Host);
        SetRefreshTime((args.Uplink, jobListingsComp));
    }

    [SubscribeLocalEvent]
    private void OnUplinkLinked(ref UplinkLinkedEvent args)
    {
        if (!TryComp<JobListingsComponent>(args.Uplink, out var jobListingsComp))
            return;

        Link((args.Uplink, jobListingsComp), args.Host);
    }
}