// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.Screens;

/// <summary>
/// Sets a screen's text to whatever value a signal has.
/// Limited "refresh rate" to avoid lag factories.
/// </summary>
[RegisterComponent, Access(typeof(SignalScreenSystem))]
[AutoGenerateComponentPause]
public sealed partial class SignalScreenComponent : Component
{
    /// <summary>
    /// Port to receive text from.
    /// Supports signal state, int and string data.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TextPort = "Text";

    /// <summary>
    /// How long to wait between handling received signals.
    /// </summary>
    [DataField]
    public TimeSpan ChangeCooldown = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextChange;
}
