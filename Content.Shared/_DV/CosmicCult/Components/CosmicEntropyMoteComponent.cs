namespace Content.Shared._DV.CosmicCult.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class CosmicEntropyMoteComponent : Component
{
    [DataField]
    public int Entropy = 1;

    [DataField]
    public EntProtoId ShatterVFX = "CleanseEffectVFX";

    [DataField]
    public SoundSpecifier ShatterSFX = new SoundPathSpecifier("/Audio/_DV/CosmicCult/cleanse_deconversion.ogg");
}
