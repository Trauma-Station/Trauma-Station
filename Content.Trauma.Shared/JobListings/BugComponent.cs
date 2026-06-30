// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// Component on that is supposed to be anchored to certain areas to fulfil objectives..
/// </summary>
[RegisterComponent]
public sealed partial class BugComponent : Component
{
    /// <summary>
    /// The area that this bug should be planted in.
    /// </summary>
    [DataField]
    public EntProtoId TargetArea;
}
