// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

public partial class ListingData
{
    [DataField]
    public bool ResetRestockOnPurchase;

    [DataField]
    public TimeSpan? RestockAfterPurchase;

    /// <summary>
    /// When purchased, it will block refunds of these listings.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ListingPrototype>> BlockRefundListings = new();

    [DataField]
    public bool RaiseProductEventOnMind;

    /// <summary>
    /// If not null, and Cost contains multiple currencies,
    /// this will instead make the listing purchaseable for either of those currencies.
    /// Currency -> Priority to spend (higher priority determines which currency will be spent, if available)
    /// Store UI display will show highest priority currency if available and lowest if not enough currency
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, int>? AltCostCurrencyPriorities;
}

public sealed partial class ListingDataWithCostModifiers
{
    /// <summary>
    /// Tracks listing cost on each purchase
    /// </summary>
    public List<Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>> PurchaseCostHistory = new();

    /// <summary>
    /// Selects a set amount of currencies from <see cref="AltCostCurrencyPriorities"> based on priority
    /// </summary>
    /// <param name="balance">Available balance.</param>
    /// <param name="skipped">Whether selecting was skipped due to <see cref="AltCostCurrencyPriorities"> not being defined.</param>
    /// <returns>null if selecting failed, otherwise selected currencies</returns>
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>?
        TryGetSelectedCurrenciesForPurchase(Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> balance,
        out bool skipped)
    {
        skipped = true;

        if (AltCostCurrencyPriorities is not { } dict || dict.Count == 0)
            return null;

        skipped = false;

        var selectedCurrencies = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();

        var sum = Cost.Sum(x => x.Value.Float());
        var remainingSum = sum;

        foreach (var (key, _) in dict.OrderByDescending(x => x.Value))
        {
            if (!Cost.TryGetValue(key, out var cost) || !balance.TryGetValue(key, out var value) || value <= 0)
                continue;

            if (cost == FixedPoint2.Zero)
            {
                selectedCurrencies[key] = FixedPoint2.Zero;
                return selectedCurrencies;
            }

            var ratio = sum / cost.Float();

            var oldRemaining = remainingSum;
            remainingSum = MathF.Max(0f, sum - value.Float() * ratio);

            if (ratio == 0f)
                return null;

            selectedCurrencies[key] = (oldRemaining - remainingSum) / ratio;
            if (remainingSum <= 0f)
                return selectedCurrencies;
        }

        return null;
    }
}
