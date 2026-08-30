// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Areas;

namespace Content.Trauma.Shared.JobListings;

/// <summary>
/// An objective to bug a specific area.
/// </summary>
[RegisterComponent]
public sealed partial class BugAreaConditionComponent : Component
{
    /// <summary>
    /// The area to bug.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<AreaComponent> TargetArea;

    /// <summary>
    /// The name the objective with this component will have.
    /// </summary>
    [DataField(required: true)]
    public LocId ObjectiveName;

    /// <summary>
    /// The description the objective with this component will have.
    /// </summary>
    [DataField(required: true)]
    public LocId ObjectiveDescription;

    /// <summary>
    /// The prototype of the entity this objective's icon should look like.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId IconEntity;
}
