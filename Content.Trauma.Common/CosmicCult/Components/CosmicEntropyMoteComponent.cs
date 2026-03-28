using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicEntropyMoteComponent : Component
{
    [DataField]
    public int Entropy = 1;

    [DataField]
    public EntProtoId ShatterVFX = "CleanseEffectVFX";

    [DataField]
    public SoundSpecifier ShatterSFX = new SoundPathSpecifier("/Audio/_Trauma/CosmicCult/cleanse_deconversion.ogg");
}
