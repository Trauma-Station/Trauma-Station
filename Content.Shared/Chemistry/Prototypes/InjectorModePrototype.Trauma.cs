using Robust.Shared.Audio;

namespace Content.Shared.Chemistry.Prototypes;

public sealed partial class InjectorModePrototype
{
    /// <summary>
    ///     Sound that will be played when drawing.
    /// </summary>
    [DataField]
    public SoundSpecifier? DrawSound;

    /// <summary>
    /// A popup for the target upon a successful draw.
    /// </summary>
    [DataField]
    public LocId? DrawPopupTarget;
}
