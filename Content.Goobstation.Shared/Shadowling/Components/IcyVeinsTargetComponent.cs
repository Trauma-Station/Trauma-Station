// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Shadowling.Components;

/// <summary>
/// Marks target as affected by Icy Veins and applies its effects
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class IcyVeinsTargetComponent : Component
{
    /// <summary>
    /// Temperature, not energy, to decrease every <see cref="DecreaseDelay"/>.
    /// </summary>
    [DataField]
    public float TempDecrease = 5f;

    /// <summary>
    /// When to end the effect
    /// </summary>
    [DataField]
    public float MinTargetTemperature = 200f;

    [DataField]
    public TimeSpan DecreaseDelay = TimeSpan.FromSeconds(0.6);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDecrease;
}
