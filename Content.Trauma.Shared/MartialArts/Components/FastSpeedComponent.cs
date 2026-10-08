// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Capoeira specific component, scales combo effects with how fast the user is moving.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FastSpeedComponent : Component
{
    [DataField]
    public float VelocityPowerMultiplier = 0.6f;

    [DataField]
    public float MinPower = 1f;

    [DataField]
    public float MaxPower = 4f;
}
