// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Component that runs <see cref="EntityConditions"/> on the performer before an action runs,
/// and cancels the action if the conditions do not pass the requirements.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionConditionsComponent : Component
{
    /// <summary>
    /// The conditions to test against.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition[] Conditions = default!;

    /// <summary>
    /// If this is true, only one condition must pass.
    /// </summary>
    [DataField]
    public bool Any;

    /// <summary>
    /// Popup that appears once the conditions fail.
    /// </summary>
    [DataField]
    public string FailPopup = "You do not meet the requirements to cast this action!";
}
