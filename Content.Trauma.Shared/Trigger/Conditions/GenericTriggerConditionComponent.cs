// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Conditions;

/// <summary>
/// Uses a <see cref="EntityCondition"/> as a trigger condition.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GenericTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    /// <summary>
    /// If true, checks the condition against the user instead of this entity.
    /// If there is no user for a trigger attempt, triggering will be prevented.
    /// </summary>
    [DataField]
    public bool CheckUser;
}
