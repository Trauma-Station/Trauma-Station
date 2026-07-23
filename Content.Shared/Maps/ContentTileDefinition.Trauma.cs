namespace Content.Shared.Maps;

public sealed partial class ContentTileDefinition
{
    /// <summary>
    /// Tile deconstruct do-after time multiplier
    /// </summary>
    [DataField]
    public float DeconstructTimeMultiplier;

    [DataField]
    public bool Reinforced;

    [DataField]
    public float TileRipResistance = 125f;
}
