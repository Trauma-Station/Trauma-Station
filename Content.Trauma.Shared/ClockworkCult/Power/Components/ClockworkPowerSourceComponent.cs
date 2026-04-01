// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// If anchored on top of a <see cref="PowerVeinComponent"/>, it starts generating power.
///
/// Requires <see cref="AnchorableComponent"/> and <see cref="BatteryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkPowerSourceComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// The rate in which the power source is getting recharged.
    /// Used to set values on <see cref="BatterySelfRechargerComponent"/>.
    /// </summary>
    [DataField]
    public float RechargeRate;

    /// <summary>
    /// How often to recharge the battery.
    /// </summary>
    [DataField]
    public TimeSpan RechargeTime;
}
