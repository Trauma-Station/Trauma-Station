// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Weather.Components;

/// <summary>
/// Makes an entity not take damage from ash storms.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherImmuneComponent : Component;
