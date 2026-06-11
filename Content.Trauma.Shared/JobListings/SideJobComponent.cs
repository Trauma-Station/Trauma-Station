// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;

namespace Content.Trauma.Shared.JobListings;

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
    public EntProtoId? Reward;

    /// <summary>
    /// The entity spawned when the side job is accepted, to be used to complete the job.
    /// For example, it could be a bug that must be planted in an office.
    /// </summary>
    public EntProtoId? Tool;
}
