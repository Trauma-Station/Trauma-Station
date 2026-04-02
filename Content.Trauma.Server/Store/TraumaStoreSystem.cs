// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Server.Store.Systems;
using Content.Shared.Database;
using Content.Shared.Store.Components;
using Content.Trauma.Common.Wizard;

namespace Content.Trauma.Server.Store;

public sealed partial class TraumaStoreSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<StoreComponent, StoreRefundAllListingsMessage>(OnRefundAll);
        SubscribeLocalEvent<StoreComponent, StoreRefundListingMessage>(OnRefundListing);
    }

    private void OnRefundListing(Entity<StoreComponent> ent, ref StoreRefundListingMessage args)
    {
        if (args.Actor is not { Valid: true } buyer)
            return;

        var (uid, component) = ent;

        var listing = GetEntity(args.ListingEntity);

        if (_store.RefundListing(uid, component, listing, buyer, true))
            _store.UpdateUserInterface(buyer, uid, component);

        _store.UpdateRefundUserInterface(uid, component);
    }

    private void OnRefundAll(Entity<StoreComponent> ent, ref StoreRefundAllListingsMessage args)
    {
        if (args.Actor is not { Valid: true } buyer)
            return;

        var (uid, component) = ent;

        if (!_store.IsOnStartingMap(uid, component) || !component.RefundAllowed || component.BoughtEntities.Count == 0)
        {
            _store.UpdateRefundUserInterface(uid, component);
            return;
        }

        _admin.Add(LogType.StoreRefund, LogImpact.Low, $"{ToPrettyString(buyer):player} has refunded their purchases from {ToPrettyString(uid):store}");

        for (var i = component.BoughtEntities.Count - 1; i >= 0; i--)
        {
            var purchase = component.BoughtEntities[i];

            _store.RefundListing(uid, component, purchase, buyer, false);
        }

        _store.UpdateUserInterface(buyer, uid, component);
        _store.UpdateRefundUserInterface(uid, component);
    }
}
