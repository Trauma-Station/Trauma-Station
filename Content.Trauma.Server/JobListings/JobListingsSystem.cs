// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Database.Migrations.Postgres;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.PDA;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.PDA;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Common.Traitor;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the side-jobs for progressive traitor.
/// </summary>
public sealed partial class JobListingsSystem : EntitySystem
{
    [Dependency] private ObjectivesSystem _objectives = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PdaSystem _pda = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private EntityTableSystem _table = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private EntityQuery<JobListingsComponent> _jobListingsQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UplinkAssignedEvent>(OnUplinkAssigned);
        SubscribeLocalEvent<UplinkLinkedEvent>(OnUplinkLinked);
        SubscribeLocalEvent<PdaComponent, PdaShowJobListingsMessage>(OnMessage);
        SubscribeLocalEvent<RemoteJobListingsComponent, JobListingsAcceptJobMessage>(OnMessage);
        SubscribeLocalEvent<RemoteJobListingsComponent, JobListingsClaimJobMessage>(OnMessage);
        SubscribeLocalEvent<RemoteJobListingsComponent, JobListingsCancelJobMessage>(OnMessage);
        SubscribeLocalEvent<RemoteJobListingsComponent, JobListingsRefreshMessage>(OnMessage);

        InitializeReward();
        InitializeRoundEnd();
    }

    /// <summary>
    /// Similar to the method on the ObjectivesSystem but with extra info for side jobs.
    /// </summary>
    public SideJobInfo? GetInfo(EntityUid mind, EntityUid sideJob)
    {
        var basic = _objectives.GetInfo(sideJob, mind);
        if (basic is null)
            return null;
        if (!TryComp<SideJobComponent>(sideJob, out var sideJobComp))
            return null;
        if (sideJobComp.Reward is null)
            return null;
        if (!_proto.Resolve(sideJobComp.Reward.Value, out var rewardProto))
            return null;

        var name = Loc.GetString($"job-listings-ui-reward-name-{rewardProto.ID}");
        return new SideJobInfo(basic.Value.Title, basic.Value.Description, basic.Value.Icon, basic.Value.Progress, name, sideJobComp.ReputationGain, GetNetEntity(sideJob));
    }

    /// <summary>
    /// Assign the store owner a random side job.
    /// When the traitor is assigned their uplink, the traitor's mind becomes the store's owner.
    /// </summary>
    public bool AssignSideJob(Entity<JobListingsComponent> jobBoard, int effectiveLevel)
    {
        if (jobBoard.Comp.Mind is null)
            return false;

        var mind = jobBoard.Comp.Mind.Value;
        if (!TryComp<MindComponent>(mind, out var mindComp))
            return false;

        var possibleJobs = jobBoard.Comp.SideJobOffers.ShallowClone();
        var possiblePriorityJobs = jobBoard.Comp.PrioritySideJobOffers.ShallowClone();
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(jobBoard.Owner));

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

            jobBoard.Comp.AvailableSideJobs.Add(sideJob);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Accept an already assigned job.
    /// </summary>
    public bool AcceptSideJob(Entity<JobListingsComponent> jobBoard, EntityUid actor, EntityUid sideJob)
    {
        if (jobBoard.Comp.AcceptedSideJobs.Count >= jobBoard.Comp.MaximumAcceptedSideJobs)
            return false;
        if (!jobBoard.Comp.AvailableSideJobs.Contains(sideJob))
            return false;
        if (!TryComp<SideJobComponent>(sideJob, out var sideJobComp))
            return false;

        jobBoard.Comp.AvailableSideJobs.Remove(sideJob);
        jobBoard.Comp.AcceptedSideJobs.Add(sideJob);

        if (sideJobComp.Tool is not null)
        {
            var reward = Spawn(sideJobComp.Tool.Value, Transform(actor).Coordinates);
            _hands.PickupOrDrop(actor, reward);
        }

        return true;
    }

    /// <summary>
    /// Cancel an already accepted job.
    /// </summary>
    public void CancelSideJob(Entity<JobListingsComponent> jobBoard, EntityUid sideJob)
    {
        jobBoard.Comp.AcceptedSideJobs.Remove(sideJob);
        QueueDel(sideJob);
    }

    /// <summary>
    /// Claim a completed job and retrieve the rewards.
    /// </summary>
    public void ClaimSideJob(Entity<JobListingsComponent> jobBoard, EntityUid actor, EntityUid sideJob)
    {
        if (jobBoard.Comp.Mind is null)
            return;
        var info = GetInfo(jobBoard.Comp.Mind.Value, sideJob);
        if (info is null)
            return;
        if (info.Value.Progress < 0.999F)
            return;
        if (!TryComp<SideJobComponent>(sideJob, out var sideJobComp))
            return;

        jobBoard.Comp.AcceptedSideJobs.Remove(sideJob);

        if (sideJobComp.Reward is not null)
        {
            var reward = Spawn(sideJobComp.Reward.Value, Transform(actor).Coordinates);
            _hands.PickupOrDrop(actor, reward);
        }

        if (!sideJobComp.Repeatable)
        {
            var availableSideJobProto = MetaData(sideJob).EntityPrototype;
            if (availableSideJobProto is not null)
                jobBoard.Comp.CompletedObjectives.Add(availableSideJobProto.ID);
        }

        GainReputation(jobBoard, sideJobComp.ReputationGain);
        jobBoard.Comp.JobsCompleted += 1;
        QueueDel(sideJob);
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
            var availableSideJobProto = MetaData(availableSideJob).EntityPrototype;
            if (availableSideJobProto is not null && availableSideJobProto.ID == sideJobProtoId)
                return false;
        }
        foreach (var acceptedSideJob in jobBoard.Comp.AcceptedSideJobs)
        {
            var acceptedSideJobProto = MetaData(acceptedSideJob).EntityPrototype;
            if (acceptedSideJobProto is not null && acceptedSideJobProto.ID == sideJobProtoId)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Count how many jobs exist on the job board.
    /// This includes both available and assigned.
    /// </summary>
    public int CountSideJobs(Entity<JobListingsComponent> jobBoard)
    {
        return jobBoard.Comp.AvailableSideJobs.Count + jobBoard.Comp.AcceptedSideJobs.Count;
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
    /// Open the job listings ui.
    /// </summary>
    public void OpenUi(EntityUid owner, EntityUid actor)
    {
        _ui.TryOpenUi(owner, JobListingsUiKey.Key, actor);
        UpdateUi(owner);
    }

    /// <summary>
    /// Update the job listings ui on an entity.
    /// </summary>
    /// <param name="owner">The entity that owns the Ui, probably a PDA.</param>
    public void UpdateUi(EntityUid owner)
    {
        if (!GetJobBoard(owner, out var jobBoard))
            return;
        if (jobBoard.Value.Comp.Mind is null)
            return;

        var availableSideJobs = new List<SideJobInfo>();
        foreach (var sideJob in jobBoard.Value.Comp.AvailableSideJobs)
        {
            var info = GetInfo(jobBoard.Value.Comp.Mind.Value, sideJob);
            if (info is null)
                continue;
            availableSideJobs.Add(info.Value);
        }

        var acceptedSideJobs = new List<SideJobInfo>();
        foreach (var sideJob in jobBoard.Value.Comp.AcceptedSideJobs)
        {
            var info = GetInfo(jobBoard.Value.Comp.Mind.Value, sideJob);
            if (info is null)
                continue;
            acceptedSideJobs.Add(info.Value);
        }

        var reputationLevel = GetReputationLevel(jobBoard.Value);

        var state = new JobListingsUserInterfaceState(availableSideJobs, acceptedSideJobs, jobBoard.Value.Comp.Reputation, reputationLevel, jobBoard.Value.Comp.MaximumAcceptedSideJobs, jobBoard.Value.Comp.BonusRefresh, jobBoard.Value.Comp.RefreshTime, jobBoard.Value.Comp.RefreshWaitDuration);
        _ui.SetUiState(owner, JobListingsUiKey.Key, state);
    }

    /// <summary>
    /// Update the entities with uis that point to this job board.
    /// </summary>
    public void UpdateUis(Entity<JobListingsComponent> jobBoard)
    {
        foreach (var remote in jobBoard.Comp.Remotes)
        {
            UpdateUi(remote);
        }
    }

    /// <summary>
    /// Update the entities with uis that point to the job board owned by this mind.
    /// </summary>
    public void UpdateUis(Entity<MindComponent> mind)
    {
        if (!TryComp<JobListingsOwnerComponent>(mind.Owner, out var jobBoard))
            return;
        if (!TryComp<JobListingsComponent>(jobBoard.JobListings, out var jobListingsComp))
            return;
        UpdateUis((jobBoard.JobListings, jobListingsComp));
    }

    /// <summary>
    /// Find a job board from an entity that has a <see cref="RemoteJobListingsComponent"/>.
    /// </summary>
    public bool GetJobBoard(EntityUid owner, [NotNullWhen(true)] out Entity<JobListingsComponent>? jobBoard)
    {
        jobBoard = null;

        if (!TryComp<RemoteJobListingsComponent>(owner, out var remoteComp))
            return false;
        if (!TryComp<JobListingsComponent>(remoteComp.JobListings, out var jobListingsComp))
            return false;

        jobBoard = (remoteComp.JobListings, jobListingsComp);
        return true;
    }

    /// <summary>
    /// Link an entity with a ui (like a pda) to a job board.
    /// </summary>
    public void Link(Entity<JobListingsComponent> jobBoard, EntityUid remote)
    {
        AddComp(remote, new RemoteJobListingsComponent { JobListings = jobBoard.Owner });
        jobBoard.Comp.Remotes.Add(remote);
    }

    /// <summary>
    /// Set the time when the refresh button on this job board will become available.
    /// </summary>
    public void SetRefreshTime(Entity<JobListingsComponent> jobBoard)
    {
        jobBoard.Comp.RefreshTime = _timing.CurTime + jobBoard.Comp.RefreshWaitDuration;
    }

    /// <summary>
    /// Determines if the job board can be refreshed at this current time.
    /// This is a final server-side check.
    /// </summary>
    public bool CanRefresh(Entity<JobListingsComponent> jobBoard)
    {
        return jobBoard.Comp.BonusRefresh || jobBoard.Comp.RefreshTime is not null && _timing.CurTime >= jobBoard.Comp.RefreshTime;
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
            QueueDel(job);
        }
        jobBoard.Comp.AvailableSideJobs.Clear();
        jobBoard.Comp.BonusRefresh = false;

        SetRefreshTime(jobBoard);
        FillSideJobs(jobBoard);
    }

    /// <summary>
    /// Work out the level (and therefore title) the traitor should have based on
    /// </summary>
    public int GetReputationLevel(Entity<JobListingsComponent> jobBoard)
    {
        var reputationLevel = 0;
        foreach (var bracket in jobBoard.Comp.ReputationLevels)
        {
            if (jobBoard.Comp.Reputation >= bracket)
                reputationLevel += 1;
            else
                break;
        }
        return reputationLevel;
    }

    /// <summary>
    /// Increase the traitor's reputation by a certain amount.
    /// Grain a bonus refresh if they level up.
    /// </summary>
    public void GainReputation(Entity<JobListingsComponent> jobBoard, int reputationGain)
    {
        var oldLevel = GetReputationLevel(jobBoard);
        jobBoard.Comp.Reputation += reputationGain;
        var newLevel = GetReputationLevel(jobBoard);
        if (newLevel > oldLevel)
            jobBoard.Comp.BonusRefresh = true;
    }

    private void OnUplinkAssigned(ref UplinkAssignedEvent args)
    {
        if (!TryComp<JobListingsComponent>(args.Uplink, out var jobListingsComp))
            return;

        var mind = _mind.GetMind(args.User);
        if (mind is null)
            return;
        jobListingsComp.Mind = mind.Value;
        AddComp(mind.Value, new JobListingsOwnerComponent { JobListings = args.Uplink });

        FillSideJobs((args.Uplink, jobListingsComp));
        Link((args.Uplink, jobListingsComp), args.Host);
        SetRefreshTime((args.Uplink, jobListingsComp));
    }

    private void OnUplinkLinked(ref UplinkLinkedEvent args)
    {
        if (!TryComp<JobListingsComponent>(args.Uplink, out var jobListingsComp))
            return;

        Link((args.Uplink, jobListingsComp), args.Host);
    }

    private void OnMessage(Entity<PdaComponent> pda, ref PdaShowJobListingsMessage msg)
    {
        OpenUi(pda, msg.Actor);
    }

    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsAcceptJobMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        AcceptSideJob(jobBoard.Value, msg.Actor, GetEntity(msg.Job));
        UpdateUi(owner.Owner);
    }

    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsClaimJobMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        ClaimSideJob(jobBoard.Value, msg.Actor, GetEntity(msg.Job));
        UpdateUi(owner.Owner);
    }

    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsCancelJobMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        CancelSideJob(jobBoard.Value, GetEntity(msg.Job));
        UpdateUi(owner.Owner);
    }

    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsRefreshMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        if (!CanRefresh(jobBoard.Value))
            return;
        Refresh(jobBoard.Value);
        UpdateUi(owner.Owner);
    }
}

/// <summary>
/// Raised on a side job when it is created.
/// </summary>
[ByRefEvent]
public record struct SideJobCreatedEvent(int EffectiveLevel, bool Cancelled = false);
