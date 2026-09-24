// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.Runes;

/// <summary>
/// Component for a rune, finished or unfinished, to be erased by a bible or ritual dagger.
/// Unfinished runes use this to allow drawing them with a ritual dagger too.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CultRuneDrawingComponent : Component
{
    /// <summary>
    /// If non-null, this is an unfinished rune which can be carved into the finished prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BloodRunePrototype>? Rune;

    /// <summary>
    /// How much longer the rune will take to draw.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimeRemaining;

    /// <summary>
    /// When the drawing doafter was started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan StartedDrawing;

    [DataField]
    public SoundSpecifier EndDrawingSound = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/blood.ogg")
    {
        Params = AudioParams.Default.WithMaxDistance(4f)
    };
}
