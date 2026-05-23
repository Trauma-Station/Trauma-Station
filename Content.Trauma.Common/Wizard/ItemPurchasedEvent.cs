// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard;

public sealed class ItemPurchasedEvent(EntityUid buyer) : EntityEventArgs
{
    public EntityUid Buyer = buyer;
}
