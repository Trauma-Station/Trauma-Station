namespace Content.Shared.Cargo.Prototypes;

public sealed partial class CargoProductPrototype
{
    /// <summary>
    /// The cooldown before this product can be bought again.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.Zero;

    /// <summary>
    /// If non-null, the station must be in one of these alert levels for this product to be bought.
    /// </summary>
    [DataField]
    public HashSet<string>? RequiredAlerts;
}
