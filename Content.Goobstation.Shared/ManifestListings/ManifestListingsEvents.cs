// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;

namespace Content.Goobstation.Shared.ManifestListings;

[ByRefEvent]
public record struct PrependObjectivesSummaryTextEvent(string Text = "");

[ByRefEvent]
public readonly record struct ListingPurchasedEvent(EntityUid User, EntityUid Store, ListingDataWithCostModifiers Data);
