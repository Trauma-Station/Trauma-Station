// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// A list of areas the traitor has bugged. This component is stored on their mind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BugMindArchiveComponent : Component
{
    /// <summary>
    /// List of bugged areas.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> BuggedAreas = new();
}
