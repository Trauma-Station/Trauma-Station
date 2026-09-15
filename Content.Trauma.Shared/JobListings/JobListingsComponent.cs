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
    /// The ID of the AvailableSideJobs container.
    /// </summary>
    [ViewVariables]
    public const string AvailableSideJobsContainerId = "available_side_jobs";

    /// <summary>
    /// The container of the side jobs which can be accepted by the traitor.
    /// </summary>
    [ViewVariables]
    public Container AvailableSideJobs = default!;

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
    public Container AcceptedSideJobs = default!;

    /// <summary>
    ///  The mind of the person (probably traitor) who owns the job board.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mind;

    /// <summary>
    /// The reputation of the traitor who owns the job board.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Reputation;

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
    public int JobsCompleted;

    /// <summary>
    /// A list of <see cref="RemoteJobListingsComponent"/> that are targeting this entity.
    /// Should only be edited by <see cref="JobListingsSystem.Link"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Remotes = new();

    /// <summary>
    /// If the job board has a bonus refresh available from leveling up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BonusRefresh;

    /// <summary>
    /// The time when the job board's refresh button becomes available.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? RefreshTime;

    /// <summary>
    /// How long it takes for the job board's refresh button to become available.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan RefreshWaitDuration;
}
