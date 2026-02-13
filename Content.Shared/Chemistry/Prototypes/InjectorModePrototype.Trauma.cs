using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

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
