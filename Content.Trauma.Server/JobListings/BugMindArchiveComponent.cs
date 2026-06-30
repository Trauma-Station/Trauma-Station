// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// A list of areas the traitor has bugged. This component is stored on their mind.
/// </summary>
[RegisterComponent]
public sealed partial class BugMindArchiveComponent : Component
{
    /// <summary>
    /// List of bugged areas.
    /// </summary>
    [DataField]
    public List<EntProtoId> BuggedAreas = new();
}
