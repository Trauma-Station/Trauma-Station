// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Component added to a store entity to enable side-jobs.
/// Used for progressive traitor.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class JobListingsComponent : Component
{
    /// <summary>
    /// How many jobs can exist at once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaximumSideJobs;

    /// <summary>
    /// List of prototypes of the objectives offered for side jobs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> SideJobOffers = new();

    /// <summary>
    /// List of prototypes of the objectives offered for side jobs, except this one is always pooled from first.
    /// For the kill objectives because otherwise they would get picked super rarely because there is 1 kill objective and 10 different steal objectives.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> PrioritySideJobOffers = new();

    /// <summary>
    /// The ID of the AvailableSideJobs container.
    /// </summary>
    [ViewVariables]
    public const string AvailableSideJobsContainerId = "available_side_jobs";

    /// <summary>
    /// The container of the side jobs which can be accepted by the traitor.
    /// </summary>
    [ViewVariables]
    public Container AvailableSideJobs;

    /// <summary>
    /// How many jobs can be accepted at once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaximumAcceptedSideJobs;

    /// <summary>
    /// How many sidejobs can be from the current highest reputation level.
    /// When you reach level 1 and start seeing major missions, we still want some minor missions.
    /// So if you set this field to 2, then you get 2 major missions and then the rest are minor missions.
    /// If you are level 2, you get 2 extreme missions then 2 major missions then the rest are minor (if there is room).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SideJobsPerLevel;

    [ViewVariables]
    public const string AcceptedSideJobsContainerId = "accepted_side_jobs";

    /// <summary>
    /// The container of the side jobs the traitor has accepted.
    /// </summary>
    [ViewVariables]
    public Container AcceptedSideJobs;

    /// <summary>
    ///  The mind of the person (probably traitor) who owns the job board.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Mind;

    /// <summary>
    /// The reputation of the traitor who owns the job board.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Reputation = 0;

    /// <summary>
    /// Reputation required to reach each level.
    /// Level 0 by default, first element in list is rep required for level 1, second element is rep required for level 2, and so on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<int> ReputationLevels;

    /// <summary>
    /// The number of jobs this job board has completed.
    /// Tracked for the end of round summary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int JobsCompleted = 0;

    /// <summary>
    /// A list of <see cref="RemoteJobListingsComponent"/> that are targeting this entity.
    /// Should only be edited by <see cref="JobListingsSystem.Link"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<NetEntity> Remotes = new();

    /// <summary>
    /// Non-repeating objectives that have already been completed.
    /// You will only be asked to steal the CE's magboots once, for example.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> CompletedObjectives = new();

    /// <summary>
    /// If the job board has a bonus refresh available from leveling up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BonusRefresh = false;

    /// <summary>
    /// The time when the job board's refresh button becomes available.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? RefreshTime;

    /// <summary>
    /// How long it takes for the job board's refresh button to become available.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RefreshWaitDuration = TimeSpan.FromMinutes(1);
}
