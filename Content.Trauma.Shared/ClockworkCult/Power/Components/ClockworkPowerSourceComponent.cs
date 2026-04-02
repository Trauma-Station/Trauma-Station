// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// If anchored on top of a <see cref="PowerVeinComponent"/>, it starts generating power.
///
/// Requires <see cref="AnchorableComponent"/> and <see cref="LimitedChargesComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkPowerSourceComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// How often to recharge the charges.
    /// </summary>
    [DataField]
    public TimeSpan RechargeTime;
}
