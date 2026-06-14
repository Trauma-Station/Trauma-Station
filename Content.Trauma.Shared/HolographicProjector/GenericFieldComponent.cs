// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.HolographicProjector;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GenericFieldComponent : Component
{
    /// <summary>
    /// The generator that made this field
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SourceGen;

    /// <summary>
    /// how much damage to heal per second
    /// </summary>
    [DataField]
    public float RegenRate = -5f;

    /// <summary>
    /// Used to check if it's healed damage recently.
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RegenTimer;

    /// <summary>
    /// How many seconds should the field wait to regenerate?
    /// </summary>
    [DataField]
    public TimeSpan RegenTime = TimeSpan.FromSeconds(1);
}
