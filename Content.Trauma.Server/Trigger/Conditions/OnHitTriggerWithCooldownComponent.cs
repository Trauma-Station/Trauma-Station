// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Conditions;


namespace Content.Trauma.Server.Trigger.Conditions;

/// <summary>
/// Checks if enought time have passed
/// </summary>
[RegisterComponent]
public sealed partial class OnHitTriggerWithCooldownComponent : BaseTriggerConditionComponent
{
    [DataField]
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(0);

    public TimeSpan LastActivated = TimeSpan.FromSeconds(0);
}
