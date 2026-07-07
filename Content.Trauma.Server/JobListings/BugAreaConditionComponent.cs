// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// An objective to bug a specific area.
/// </summary>
[RegisterComponent]
public sealed partial class BugAreaConditionComponent : Component
{
    /// <summary>
    /// The area to bug.
    /// </summary>
    [DataField]
    public EntProtoId TargetArea;

    /// <summary>
    /// The name the objective with this component will have.
    /// </summary>
    [DataField]
    public LocId ObjectiveName;

    /// <summary>
    /// The description the objective with this component will have.
    /// </summary>
    [DataField]
    public LocId ObjectiveDescription;

    /// <summary>
    /// The prototype of the entity this objective's icon should look like.
    /// </summary>
    [DataField]
    public EntProtoId IconEntity;
}
