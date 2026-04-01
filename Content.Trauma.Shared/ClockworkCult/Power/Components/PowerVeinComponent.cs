// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// Enables the use of <see cref="ClockworkPowerSourceComponent"/>,
/// if it's anchored on top of an entity with this component.
///
/// Requires <see cref="AreaComponent"/> to function.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerVeinComponent : Component;
