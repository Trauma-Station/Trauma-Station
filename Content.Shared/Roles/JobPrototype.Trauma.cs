using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;
public sealed partial class JobPrototype
{
    /// <summary>
    /// Amount job starts with currency in the bank.
    /// </summary>
    [DataField]
    public FixedPoint2 StartingCurrency = 20.05;

    /// <summary>
    /// The type of starting currency of bank to grab from. (i.e. Spesos or TC or whatever).
    /// </summary>
    [DataField]
    public ProtoId<CurrencyPrototype> StartingCurrencyType = "Spesos";

}
