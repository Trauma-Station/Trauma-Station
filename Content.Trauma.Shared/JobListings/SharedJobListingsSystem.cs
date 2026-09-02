// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Trauma.Common.JobListings;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// System that manages the side-jobs for progressive traitor.
/// </summary>
public abstract partial class SharedJobListingsSystem : EntitySystem
{
    [Dependency] protected SharedObjectivesSystem Objectives = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedUserInterfaceSystem Ui = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] protected SharedMindSystem Mind = default!;
    [Dependency] protected SharedHandsSystem Hands = default!;
    [Dependency] protected EntityQuery<JobListingsComponent> JobListingsQuery = default!;
    [Dependency] protected EntityQuery<SideJobComponent> SideJobQuery = default!;
    [Dependency] protected EntityQuery<MindComponent> MindQuery = default!;
    [Dependency] protected EntityQuery<ObjectiveComponent> ObjectiveQuery = default!;
    [Dependency] private EntityQuery<JobListingsOwnerComponent> _jobListingsOwnerQuery = default!;

    /// <summary>
    /// Accept an already assigned job.
    /// </summary>
    public bool AcceptSideJob(Entity<JobListingsComponent> jobBoard, EntityUid actor, EntityUid sideJob)
    {
        if (jobBoard.Comp.AcceptedSideJobs.Count >= jobBoard.Comp.MaximumAcceptedSideJobs)
            return false;
        if (!jobBoard.Comp.AvailableSideJobs.Contains(sideJob))
            return false;
        if (!SideJobQuery.TryComp(sideJob, out var sideJobComp))
            return false;

        Container.Remove(sideJob, jobBoard.Comp.AvailableSideJobs);
        Container.Insert(sideJob, jobBoard.Comp.AcceptedSideJobs);

        if (sideJobComp.Tool is { } toolProto)
        {
            var tool = PredictedSpawnAtPosition(toolProto, Transform(actor).Coordinates);
            Hands.PickupOrDrop(actor, tool);
            var ev = new SideJobToolSpawned(sideJob);
            RaiseLocalEvent(tool, ref ev);
        }

        return true;
    }

    /// <summary>
    /// Cancel an already accepted job.
    /// </summary>
    public void CancelSideJob(Entity<JobListingsComponent> jobBoard, EntityUid sideJob)
    {
        Container.Remove(sideJob, jobBoard.Comp.AcceptedSideJobs);
        PredictedQueueDel(sideJob);
    }

    /// <summary>
    /// Claim a completed job and retrieve the rewards.
    /// </summary>
    public void ClaimSideJob(Entity<JobListingsComponent> jobBoard, EntityUid actor, EntityUid sideJob)
    {
        if (jobBoard.Comp.Mind is not { } netMind)
            return;
        var mind = GetEntity(netMind);
        if (!MindQuery.TryComp(mind, out var mindComp))
            return;
        var progress = Objectives.GetProgress(sideJob, (mind, mindComp));
        if (progress < 0.999f)
            return;
        if (!SideJobQuery.TryComp(sideJob, out var sideJobComp))
            return;

        Container.Remove(sideJob, jobBoard.Comp.AcceptedSideJobs);

        if (sideJobComp.Reward is not null)
        {
            var reward = PredictedSpawnAtPosition(sideJobComp.Reward.Value, Transform(actor).Coordinates);
            Hands.PickupOrDrop(actor, reward);
        }

        if (!sideJobComp.Repeatable)
        {
            var availableSideJobProto = Prototype(sideJob);
            if (availableSideJobProto is not null)
                jobBoard.Comp.CompletedObjectives.Add(availableSideJobProto.ID);
        }

        GainReputation(jobBoard, sideJobComp.ReputationGain);
        jobBoard.Comp.JobsCompleted += 1;
        DirtyField(jobBoard.AsNullable(), nameof(JobListingsComponent.JobsCompleted));
        PredictedQueueDel(sideJob);
    }

    /// <summary>
    /// A helper method to get important info about a side job.
    /// Called by the Ui to display side job information.
    /// </summary>
    public bool GetInfo(EntityUid sideJob, Entity<JobListingsComponent> jobBoard, [NotNullWhen(true)] out SideJobInfo? info)
    {
        info = null;

        var mind = GetEntity(jobBoard.Comp.Mind);
        if (mind is null)
            return false;
        if (!ObjectiveQuery.TryComp(sideJob, out var objectiveComp))
            return false;
        if (!SideJobQuery.TryComp(sideJob, out var sideJobComp))
            return false;
        if (sideJobComp.Reward is null || sideJobComp.RewardName is null)
            return false;
        if (objectiveComp.Icon is null)
            return false;

        // don't use SharedObjectiveSystem.GetInfo because it will error on the client since progress is not predicted
        var meta = MetaData(sideJob);
        var title = meta.EntityName;
        var description = meta.EntityDescription;
        var icon = objectiveComp.Icon;
        var rewardName = sideJobComp.RewardName;
        info = new SideJobInfo(GetNetEntity(sideJob), sideJobComp.CachedProgress, title, description, icon, rewardName, sideJobComp.ReputationGain);
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
    /// Open the job listings ui.
    /// </summary>
    public void OpenUi(EntityUid owner, EntityUid actor)
    {
        UpdateUi(owner, actor);
        Ui.TryOpenUi(owner, JobListingsUiKey.Key, actor);
    }

    /// <summary>
    /// Cache the side job's progress and replicate it to the client.
    /// Can only be done by the server because too much objectives are server-side.
    /// </summary>
    protected virtual void UpdateAllSideJobs(Entity<JobListingsComponent> jobBoard)
    {

    }

    /// <summary>
    /// Updates the job listings ui.
    /// </summary>
    public void UpdateUi(EntityUid owner, EntityUid actor, bool loading = false)
    {
        if (!GetJobBoard(owner, out var jobBoard))
            return;

        UpdateAllSideJobs(jobBoard.Value);

        var availableSideJobInfos = new List<SideJobInfo>();
        foreach (var sideJob in jobBoard.Value.Comp.AvailableSideJobs.ContainedEntities)
        {
            if (GetInfo(sideJob, jobBoard.Value, out var info))
                availableSideJobInfos.Add(info.Value);
        }

        var acceptedSideJobsInfos = new List<SideJobInfo>();
        foreach (var sideJob in jobBoard.Value.Comp.AcceptedSideJobs.ContainedEntities)
        {
            if (GetInfo(sideJob, jobBoard.Value, out var info))
                acceptedSideJobsInfos.Add(info.Value);
        }

        var state = new JobListingsBUI(
            availableSideJobInfos,
            acceptedSideJobsInfos,
            jobBoard.Value.Comp.Reputation,
            GetReputationLevel(jobBoard.Value),
            jobBoard.Value.Comp.BonusRefresh,
            jobBoard.Value.Comp.RefreshTime,
            jobBoard.Value.Comp.RefreshWaitDuration,
            jobBoard.Value.Comp.MaximumAcceptedSideJobs,
            loading
        );

        Ui.SetUiState(owner, JobListingsUiKey.Key, state);
    }

    /// <summary>
    /// Update all the uis of the remotes (pdas, uplink implants) of a job board.
    /// </summary>
    public void UpdateUi(Entity<JobListingsComponent> jobBoard, EntityUid actor)
    {
        foreach (var remote in jobBoard.Comp.Remotes)
        {
            UpdateUi(GetEntity(remote), actor);
        }
    }

    /// <summary>
    /// Update all the uis of the remotes (pdas, uplink implants) of a job board owned by a particular mind.
    /// </summary>
    public void UpdateUi(Entity<MindComponent> mind)
    {
        if (mind.Comp.OwnedEntity is null)
            return;
        if (!TryComp<JobListingsOwnerComponent>(mind.Owner, out var jobListingsOwnerComp))
            return;
        var jobBoard = GetEntity(jobListingsOwnerComp.JobListings);
        if (!TryComp<JobListingsComponent>(jobBoard, out var jobBoardComp))
            return;

        UpdateUi((jobBoard, jobBoardComp), mind.Comp.OwnedEntity.Value);
    }

    /// <summary>
    /// Find a job board from an entity that has a <see cref="RemoteJobListingsComponent"/>.
    /// </summary>
    public bool GetJobBoard(EntityUid owner, [NotNullWhen(true)] out Entity<JobListingsComponent>? jobBoard)
    {
        jobBoard = null;

        if (!_jobListingsOwnerQuery.TryComp(owner, out var remoteComp))
            return false;
        var jobListings = GetEntity(remoteComp.JobListings);
        if (!JobListingsQuery.TryComp(jobListings, out var jobListingsComp))
            return false;

        jobBoard = (jobListings, jobListingsComp);
        return true;
    }

    /// <summary>
    /// Setup the Ui key for the job board Ui.
    /// </summary>
    public void InitUi(Entity<JobListingsComponent> jobBoard, EntityUid host)
    {
        Ui.SetUi(host, JobListingsUiKey.Key, new InterfaceData("JobListingsBUI"));
    }

    /// <summary>
    /// Link an entity with a ui (like a pda) to a job board.
    /// </summary>
    public void Link(Entity<JobListingsComponent> jobBoard, EntityUid remote)
    {
        AddComp(remote, new RemoteJobListingsComponent { JobListings = GetNetEntity(jobBoard.Owner) });
        InitUi(jobBoard, remote);
        jobBoard.Comp.Remotes.Add(GetNetEntity(remote));
        DirtyField(jobBoard.AsNullable(), nameof(JobListingsComponent.Remotes));
    }

    /// <summary>
    /// Set the time when the refresh button on this job board will become available.
    /// </summary>
    public void SetRefreshTime(Entity<JobListingsComponent> jobBoard)
    {
        jobBoard.Comp.RefreshTime = Timing.CurTime + jobBoard.Comp.RefreshWaitDuration;
        DirtyField(jobBoard.AsNullable(), nameof(JobListingsComponent.RefreshTime));
    }

    /// <summary>
    /// Determines if the job board can be refreshed at this current time.
    /// This is a final server-side check.
    /// </summary>
    public bool CanRefresh(Entity<JobListingsComponent> jobBoard)
    {
        return jobBoard.Comp.BonusRefresh || jobBoard.Comp.RefreshTime is not null && Timing.CurTime >= jobBoard.Comp.RefreshTime;
    }

    /// <summary>
    /// Refresh the job board.
    /// This has no checks and should only be called if <see cref="CanRefresh"/> returns true.
    /// This method deletes every job not currently accepted and then assigns jobs until the job board is full.
    /// </summary>
    public virtual void Refresh(Entity<JobListingsComponent> jobBoard)
    {
        foreach (var sideJob in jobBoard.Comp.AcceptedSideJobs.ContainedEntities)
        {
            PredictedQueueDel(sideJob);
        }
        Container.EmptyContainer(jobBoard.Comp.AcceptedSideJobs);
        jobBoard.Comp.BonusRefresh = false;
        DirtyField(jobBoard, jobBoard.Comp, nameof(JobListingsComponent.BonusRefresh));
        SetRefreshTime(jobBoard);
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
        DirtyFields(jobBoard.AsNullable(), null, nameof(JobListingsComponent.Reputation), nameof(JobListingsComponent.BonusRefresh));
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<JobListingsComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.AvailableSideJobs = Container.EnsureContainer<Container>(ent, JobListingsComponent.AvailableSideJobsContainerId);
        ent.Comp.AcceptedSideJobs = Container.EnsureContainer<Container>(ent, JobListingsComponent.AcceptedSideJobsContainerId);
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<PdaComponent> pda, ref PdaShowJobListingsMessage msg)
    {
        OpenUi(pda, msg.Actor);
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsAcceptJobMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        AcceptSideJob(jobBoard.Value, msg.Actor, GetEntity(msg.Job));
        UpdateUi(owner.Owner, msg.Actor);
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsClaimJobMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        ClaimSideJob(jobBoard.Value, msg.Actor, GetEntity(msg.Job));
        UpdateUi(owner.Owner, msg.Actor);
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsCancelJobMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        CancelSideJob(jobBoard.Value, GetEntity(msg.Job));
        UpdateUi(owner.Owner, msg.Actor);
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<RemoteJobListingsComponent> owner, ref JobListingsRefreshMessage msg)
    {
        if (!GetJobBoard(owner.Owner, out var jobBoard))
            return;
        if (!CanRefresh(jobBoard.Value))
            return;
        Refresh(jobBoard.Value);
        UpdateUi(owner.Owner, msg.Actor, loading: _net.IsClient);
    }
}

/// <summary>
/// Raised on a side job when it is created.
/// </summary>
[ByRefEvent]
public record struct SideJobCreatedEvent(int EffectiveLevel, bool Cancelled = false);

/// <summary>
/// Raised on a side job's tool when it is spawned.
/// </summary>
[ByRefEvent]
public record struct SideJobToolSpawned(EntityUid Objective);
