// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Added to the uplink implant entity to provide another action to open the job listings ui.
/// </summary>
[RegisterComponent]
public sealed partial class JobListingsImplantComponent : Component
{
    /// <summary>
    /// Action gained when this component is on an implanted entity.
    /// </summary>
    [DataField]
    public EntProtoId Action;

    /// <summary>
    /// When the action is created it is stored here so it can be removed upon implant removal.
    /// </summary>
    [DataField]
    public EntityUid? StoredAction;
}
