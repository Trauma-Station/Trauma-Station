namespace Content.Shared.Nutrition.Prototypes;

public sealed partial class SatiationPrototype
{
    /// <summary>
    /// If non-null, sets the starting value explicitly instead of using random tiers.
    /// </summary>
    [DataField]
    public int? StartingValue;
}
