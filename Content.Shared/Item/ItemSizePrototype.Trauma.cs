namespace Content.Shared.Item;

/// <summary>
/// Trauma - add CrawlSpeedModifier for crawling logic
/// </summary>
public sealed partial class ItemSizePrototype
{
    /// <summary>
    /// Modifier applied to crawling speed for entities holding an item with this size.
    /// Multiplicative, so holding 2 items with 50% speed makes you crawl at 25% speed.
    /// </summary>
    [DataField]
    public float CrawlSpeedModifier = 1f;
}
