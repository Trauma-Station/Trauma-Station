// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// A component attached to an objective entity to make it into a side job.
/// Every side job has this component.
/// </summary>
[RegisterComponent]
public sealed partial class SideJobComponent : Component
{
    /// <summary>
    /// The entity spawned as a reward for completing this side job.
    /// </summary>
    [DataField]
    public EntProtoId? Reward;

    /// <summary>
    /// The entity spawned when the side job is accepted, to be used to complete the job.
    /// For example, it could be a bug that must be planted in an office.
    /// </summary>
    [DataField]
    public EntProtoId? Tool;

    /// <summary>
    /// How much reputation you gain from completing the mission.
    /// </summary>
    [DataField]
    public int ReputationGain;

    /// <summary>
    /// The minimum reputation level you must have for this job to be offered.
    /// </summary>
    [DataField]
    public int MinimumLevel;

    /// <summary>
    /// If this side job can be repeated. Theft objectives can't be repeated while murder objectives can be.
    /// </summary>
    [DataField]
    public bool Repeatable = false;
}
