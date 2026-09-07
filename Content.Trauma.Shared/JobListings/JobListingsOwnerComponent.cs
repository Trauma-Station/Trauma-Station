// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// A component attached to the mind of a job board owner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JobListingsOwnerComponent : Component
{
    /// <summary>
    /// The job board entity this mind owns.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity JobListings;
}
