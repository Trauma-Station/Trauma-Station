// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.LightDetection;

/// <summary>
/// Component that deletes an entity after exposure to light for some specific seconds.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DeleteOnLightExposureComponent : Component
{
    /// <summary>
    /// Minimum light level required for the <see cref="Update"/> to start counting.
    /// </summary>
    [DataField]
    public float LightLevel = 0.5f;

    /// <summary>
    /// If its active, and we are on light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// How long the entity must be on light to trigger the deletion.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan Update;
}
