// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Anger.Components;

/// <summary>
/// Scales HP or Anger amount depending on the amount of aggressors this entity has.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AngerPlayerScalingComponent : Component
{
    [DataField("angerScale")]
    public float? AngerScalingFactor;

    [DataField("hpScale")]
    public float? HealthScalingFactor;
}
