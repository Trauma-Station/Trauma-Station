using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Humanoid;

/// <summary>
/// Coloration strategy that only returns white
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class NoColoration : ISkinColorationStrategy
{
    public SkinColorationStrategyInput InputType
        => SkinColorationStrategyInput.NoColor;

    public bool VerifySkinColor(Color color)
        => true;

    public Color ClosestSkinColor(Color color)
        => Color.White;
}
