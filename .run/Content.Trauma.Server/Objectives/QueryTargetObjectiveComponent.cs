// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Sets an objective's target entity to the first entity that has a set component.
/// </summary>
[RegisterComponent]
public sealed partial class QueryTargetObjectiveComponent : Component
{
    /// <summary>
    /// The component to query entities for.
    /// The first one found is used, as long as isn't paused.
    /// </summary>
    [DataField(required: true)]
    public CompName Comp;
}
