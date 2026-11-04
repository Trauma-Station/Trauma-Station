// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// A component attached to the mind of an uuplink owner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UplinkOwnerComponent : Component
{
    /// <summary>
    /// The remote entities that access this uplink.
    /// These include PDA, uplink implant, e.t.c.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Remotes = new();
}
