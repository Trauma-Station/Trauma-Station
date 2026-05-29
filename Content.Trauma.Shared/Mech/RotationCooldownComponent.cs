// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Mech;

/// <summary>
/// Component for mechs that prevents instantly rotating when you click something.
/// You have to wait a delay to keep rotating.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RotationCooldownSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RotationCooldownComponent : Component
{
    /// <summary>
    /// When you can next rotate.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRotate;

    /// <summary>
    /// How long you have to wait between rotations.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Optional sound to play after rotating.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
