// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Shared.FixedPoint;
using Content.Goobstation.Shared.ManifestListings;
using Content.Shared.Mind;
using Content.Shared.Store;

namespace Content.Goobstation.Server.ManifestListings;

public sealed partial class ManifestListingsSystem : EntitySystem
{
    private StringBuilder _sb = new();
    private StringBuilder _sbIntermediate = new();

    [SubscribeLocalEvent]
    private void OnPurchase(Entity<MindComponent> ent, ref ListingPurchasedEvent args)
    {
        var listings = EnsureComp<MindListingsComponent>(ent);

        if (!listings.Listings.TryGetValue(args.Store.Id, out var list))
        {
            list = new();
            listings.Listings.Add(args.Store.Id, list);
        }

        var data = args.Data;
        list.RemoveAll(x => x.ID == data.ID);
        list.Add(data);
    }

    [SubscribeLocalEvent]
    private void OnPrepend(Entity<MindListingsComponent> ent, ref PrependObjectivesSummaryTextEvent args)
    {
        _sb.Clear();
        _sb.AppendLine();
        _sb.AppendLine();
        _sbIntermediate.Clear();

        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> totalSpent = new();
        foreach (var list in ent.Comp.Listings.Values)
        {
            Dictionary<string, ListingDataWithCostModifiers> ignoredIds = new();
            // Data id -> amount purchased (needed for action upgrades)
            Dictionary<string, int> info = new();
            foreach (var data in list)
            {
                if (data.PurchaseAmount <= 0)
                    continue;

                if (!info.TryAdd(data.ID, data.PurchaseAmount))
                    info[data.ID] += data.PurchaseAmount;

                if (data.ProductUpgradeId == null)
                    continue;

                var upgrade = list.FirstOrDefault(x => x.ID == data.ProductUpgradeId);
                if (upgrade != null)
                {
                    // This assumes each upgrade corresponds to a single listing
                    ignoredIds[data.ProductUpgradeId] = upgrade;
                    info[data.ID] += upgrade.PurchaseAmount;
                }
            }

            foreach (var (dataId, count) in info)
            {
                if (ignoredIds.ContainsKey(dataId))
                    continue;

                var data = list.FirstOrDefault(x => x.ID == dataId);
                if (data == null)
                    continue;

                Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> totalCost = new();

                foreach (var cost in data.PurchaseCostHistory)
                {
                    foreach (var (currency, amount) in cost)
                    {
                        if (amount <= FixedPoint2.Zero)
                            continue;

                        if (!totalCost.TryAdd(currency, amount))
                            totalCost[currency] += amount;
                    }
                }

                if (data.ProductUpgradeId != null && ignoredIds.TryGetValue(data.ProductUpgradeId, out var upgrade))
                {
                    foreach (var cost in upgrade.PurchaseCostHistory)
                    {
                        foreach (var (currency, amount) in cost)
                        {
                            if (amount <= FixedPoint2.Zero)
                                continue;

                            if (!totalCost.TryAdd(currency, amount))
                                totalCost[currency] += amount;
                        }
                    }
                }

                string sprite;
                var state = "";
                switch (data.Icon)
                {
                    case SpriteSpecifier.Texture tex:
                        sprite = tex.TexturePath.ToString();
                        if (!sprite.StartsWith("/Textures/"))
                            sprite = $"/Textures/{sprite}";
                        break;
                    case SpriteSpecifier.Rsi rsi:
                        sprite = rsi.RsiPath.ToString();
                        state = rsi.RsiState;
                        break;
                    default:
                        sprite = data.ProductEntity
                            ?? data.ProductAction
                            ?? ent.Comp.DefaultTexture.TexturePath.ToString();
                        break;
                }

                var name = "";
                if (data.Name != null)
                    name = Loc.GetString(data.Name);
                else
                {
                    if (data.ProductEntity != null)
                        name = Loc.GetString(ProtoMan.Index(data.ProductEntity.Value).Name);
                    else if (data.ProductAction != null)
                        name = Loc.GetString(ProtoMan.Index(data.ProductAction.Value).Name);
                }

                _sbIntermediate.Clear();
                _sbIntermediate.Append(name);
                if (totalCost.Count > 0)
                {
                    _sbIntermediate.Append(" - ");
                    foreach (var (currencyId, amount) in totalCost)
                    {
                        if (!totalSpent.TryAdd(currencyId, amount))
                            totalSpent[currencyId] += amount;

                        var currency = ProtoMan.Index(currencyId);
                        _sbIntermediate.Append(amount);
                        _sbIntermediate.Append(' ');
                        _sbIntermediate.Append(Loc.GetString(currency.DisplayName));
                        _sbIntermediate.Append(", ");
                    }

                    _sbIntermediate.Remove(_sbIntermediate.Length - 2, 2);
                }

                var information = _sbIntermediate.ToString();
                information = information.Replace("\"", ""); // Fuck this
                information = information.Replace("\'", ""); // Fuck this

                _sb.Append(Loc.GetString("manifest-listing-entry-listing",
                    ("sprite", sprite),
                    ("state", state),
                    ("info", information),
                    ("amount", count)));
            }
        }

        _sbIntermediate.Clear();
        var prependText = string.Empty;
        if (totalSpent.Count > 0)
        {
            foreach (var (currencyId, amount) in totalSpent)
            {
                if (_sbIntermediate.Length > 0)
                    _sbIntermediate.Append(", ");

                var currency = ProtoMan.Index(currencyId);
                _sbIntermediate.Append(amount);
                _sbIntermediate.Append(" ");
                _sbIntermediate.Append(Loc.GetString(currency.DisplayName));
            }

            prependText = Loc.GetString("manifest-listing-entry-start", ("spent", _sbIntermediate.ToString()));
        }

        args.Text += prependText + _sb.ToString();
    }
}
