using System.Data.Common;
using System.Linq;
using System.Text;
using Content.Shared.Store;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Store.Ui;

public sealed partial class StoreMenu
{
    public bool JobListingsSelected = false;

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

    public void SetJobListingsButtonVisibility(bool visible)
    {
        JobListingsButton.Visible = visible;
    }

    private void OnJobListingsButtonPressed(BaseButton.ButtonEventArgs args)
    {
        JobListingsSelected = true;
        JobListingsAcceptedLabel.Visible = true;
        if (JobListingsAcceptedContainer.ChildCount > 0)
            JobListingsAcceptedContainer.Visible = true;
        JobListingsAvailableLabel.Visible = true;
        if (JobListingsAvailableContainer.ChildCount > 0)
            JobListingsAvailableContainer.Visible = true;
        StoreListingsContainer.Visible = false;
        UpdateListing();

        foreach (var child in CategoryListContainer.Children)
        {
            if (child is StoreCategoryButton button)
                button.Pressed = false;
        }
    }

    public void UnpressJobListingsButton()
    {
        JobListingsSelected = false;
        JobListingsButton.Pressed = false;
        JobListingsAcceptedLabel.Visible = false;
        JobListingsAcceptedContainer.Visible = false;
        JobListingsAvailableLabel.Visible = false;
        JobListingsAvailableContainer.Visible = false;
        StoreListingsContainer.Visible = true;
    }
}
