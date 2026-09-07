// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StoreDiscount.Systems;
using Content.Shared.Store.Components;
using Content.Trauma.Shared.Store;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Server.Store;

public sealed partial class StoreSalesRefreshSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<StoreComponent> _storeQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreSalesRefreshComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO: change to new engine thing when its merged
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StoreSalesRefreshComponent>();
        foreach (var ent in query)
        {
            if (now < ent.Comp.NextRefresh)
                continue;

            ent.Comp.NextRefresh = now + ent.Comp.Delay;
            Dirty(ent);

            if (!_storeQuery.TryComp(ent, out var store))
                continue;

            // clear existing discounts or it will die
            var listings = store.FullListingsCatalog.ToList();
            foreach (var listing in listings)
            {
                if (listing.Categories.Remove(StoreDiscountSystem.DiscountedStoreCategoryPrototypeKey))
                    listing.CostModifiersBySourceId.Clear();
            }
            // it has no API and discounts is the only thing using this event :)
            var ev = new StoreInitializedEvent(ent, ent, true, listings);
            RaiseLocalEvent(ref ev);
        }
    }

    private void OnMapInit(Entity<StoreSalesRefreshComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRefresh = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent);
    }
}
