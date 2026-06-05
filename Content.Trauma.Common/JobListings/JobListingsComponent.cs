// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Trauma.Common.JobListings;

/// <summary>
/// Component added to a store entity to enable side-jobs.
/// Used for progressive traitor.
/// </summary>

[RegisterComponent]
public sealed partial class JobListingsComponent : Component
{
    /// <summary>
    /// How many jobs are offered at once.
    /// </summary>
    [DataField]
    public int JobCount;

    /// <summary>
    /// List of prototypes of the objectives offered.
    /// </summary>
    [DataField]
    public List<EntProtoId> SideJobs;

    /// <summary>
    /// The container for the entities for available side jobs which can be accepted by the traitor.
    /// </summary>
    [ViewVariables]
    public Container AvailableSideJobsContainer = default!;

    /// <summary>
    /// ID of the available side jobs container.
    /// </summary>
    [DataField]
    public string AvailableSideJobsContainerId = "available-side-jobs";
}
