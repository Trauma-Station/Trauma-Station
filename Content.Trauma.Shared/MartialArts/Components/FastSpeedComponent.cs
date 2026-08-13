// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Capoeira specific component, scales combo effects with how fast the user is moving.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FastSpeedComponent : Component
{
    /// <summary>
    /// Velocity in m/s is multiplied by this to get the power multiplier.
    /// </summary>
    [DataField]
    public float VelocityPowerMultiplier = 0.6f;

    /// <summary>
    /// Power can never go below this, so standing still is not a penalty.
    /// </summary>
    [DataField]
    public float MinPower = 1f;

    /// <summary>
    /// Power is capped here no matter how fast the user is going.
    /// </summary>
    [DataField]
    public float MaxPower = 4f;
}
