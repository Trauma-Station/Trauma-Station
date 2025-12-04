// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Conditions;

/// <summary>
/// Checks the user's access against this entity's <c>AccessReader</c>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AccessTriggerConditionComponent : BaseTriggerConditionComponent;
