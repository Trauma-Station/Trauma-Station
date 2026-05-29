namespace Content.Shared.Nutrition.Components;

public sealed partial class IngestionBlockerComponent
{
    /// <summary>
    /// Blocks smoke inhalation when this mask is down, even if internals are off.
    /// </summary>
    [DataField]
    public bool BlockSmokeIngestion;
}
