// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Component which points to a remote job listings entity.
/// The traitor pda has a RemoteStoreComponent which points to an intangible nullspace entity with the actualy uplink store on it.
/// With side jobs, the JobListingsComponent is also on that store entity and which component is on the pda uplink.
/// </summary>
[RegisterComponent]
public sealed partial class RemoteJobListingsComponent : Component
{
    /// <summary>
    /// The entity with the job listings.
    /// </summary>
    [DataField]
    public EntityUid JobListings;
}
