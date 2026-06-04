using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

public sealed partial class StoreUpdateState
{
    public readonly bool ShowJobListings = false;

    public StoreUpdateState(HashSet<ListingDataWithCostModifiers> listings, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> balance, bool showFooter, bool allowRefund, bool showJobListings) : this(listings, balance, showFooter, allowRefund)
    {
        ShowJobListings = showJobListings;
    }
}
