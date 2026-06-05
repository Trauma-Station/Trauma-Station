// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Store;

/// <summary>
/// Component added to a store entity to enable side-jobs.
/// Used for progressive traitor.
/// </summary>

[RegisterComponent, NetworkedComponent]
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
}
