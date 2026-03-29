namespace Content.Shared.Cargo;

public sealed partial class CargoOrderData
{
    /// <summary>
    /// The cooldown in seconds before this product can be bought again.
    /// </summary>
    [DataField]
    public int Cooldown;

    /// <summary>
    /// If non-null, the station must be in one of these alert levels for this product to be bought
    /// </summary>
    [DataField]
    public HashSet<string>? RequiredAlerts;
}
