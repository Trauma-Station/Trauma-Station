// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Weather.Components;

/// <summary>
/// Makes an entity not take damage from any weather.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherImmuneComponent : Component;
