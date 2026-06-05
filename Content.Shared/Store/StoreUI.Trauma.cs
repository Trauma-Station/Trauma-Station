using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

public sealed partial class StoreUpdateState
{
    public readonly bool ShowJobListings = false;
    public readonly List<NetEntity> AvailableSideJobs = new();

    public StoreUpdateState(HashSet<ListingDataWithCostModifiers> listings, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> balance, bool showFooter, bool allowRefund, bool showJobListings, List<NetEntity> availableSideJobs) : this(listings, balance, showFooter, allowRefund)
    {
        ShowJobListings = showJobListings;
        AvailableSideJobs = availableSideJobs;
    }
}
