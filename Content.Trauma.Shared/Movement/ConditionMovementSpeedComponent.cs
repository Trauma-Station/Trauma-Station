// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.Movement;

/// <summary>
/// Component that modifies movement speed based on conditions
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConditionMovementSpeedComponent : Component
{
    /// <summary>
    /// Movement speed (walk = X, sprint = Y) with conditions to check against
    /// </summary>
    [DataField]
    public Dictionary<Vector2, EntityCondition[]> Conditions;

    /// <summary>
    /// Whether to check for any condition, instead of all.
    /// </summary>
    [DataField]
    public bool Any;
}
