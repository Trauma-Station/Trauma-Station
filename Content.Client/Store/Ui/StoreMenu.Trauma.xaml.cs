using System.Linq;
using System.Text;
using Content.Shared.Store;

namespace Content.Client.Store.Ui;

public sealed partial class StoreMenu
{
    private string? GetListingAltPriceString(ListingDataWithCostModifiers listing)
    {
        var selected = listing.TryGetSelectedCurrenciesForPurchase(Balance, out var skipped);
        if (skipped)
            return null;

        if (selected is not { } sel)
        {
            var dict = listing.AltCostCurrencyPriorities!.Where(x => listing.Cost.ContainsKey(x.Key)).ToDictionary();
            if (dict.Count == 0)
                return string.Empty;

            var lowestPriority = dict.MinBy(x => x.Value).Key;
            var currency = _prototypeManager.Index(lowestPriority);
            var amount = listing.Cost[lowestPriority];
            return Loc.GetString(
                "store-ui-price-display",
                ("amount", amount),
                ("currency", Loc.GetString(currency.DisplayName, ("amount", amount)))
            );
        }
        else
        {
            StringBuilder sb = new();
            foreach (var (type, amount) in sel)
            {
                var currency = _prototypeManager.Index(type);

                sb.Append(Loc.GetString(
                    "store-ui-price-display",
                    ("amount", amount),
                    ("currency", Loc.GetString(currency.DisplayName, ("amount", amount)))
                ));

                sb.Append(' ');
            }

            return sb.Remove(sb.Length - 1, 1).ToString();
        }
    }
}
