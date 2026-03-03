using Robust.Shared.Audio;

namespace Content.Shared.Labels.Components;

/// <summary>
/// Trauma - Label print sound
/// </summary>
public sealed partial class HandLabelerComponent
{
    /// <summary>
    /// Sound played when applying a label
    /// </summary>
    [DataField]
    public SoundPathSpecifier PrintSound = new SoundPathSpecifier("/Audio/_Goobstation/Items/hand_labeler_print.ogg")
    {
        Params = AudioParams.Default.WithVolume(10f)
    };
}
