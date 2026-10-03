// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Sprite;

/// <summary>
/// Sets an appearance data enum to true while this component is added.
/// Removes itself automatically after a delay.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class DelayedVisualsComponent : Component
{
    /// <summary>
    /// How long to wait before the visual is reset.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The appearance enum to set to true when the component inits, false when it finishes.
    /// </summary>
    [DataField(required: true)]
    public Enum Key;

    /// <summary>
    /// When to set the appearance enum to false.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan Finished;
}
