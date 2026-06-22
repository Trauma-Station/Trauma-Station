// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// The list of grand theft items the scanalyzer has scanned are stored on the traitor's mind.
/// </summary>
[RegisterComponent]
public sealed partial class ScanalyzerMindArchiveComponent : Component
{
    /// <summary>
    /// List of scanned grand theft items.
    /// </summary>
    [DataField]
    public List<ProtoId<StealTargetGroupPrototype>> ScannedStealTargetGroups = new();
}
