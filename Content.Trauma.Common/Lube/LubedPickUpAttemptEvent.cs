// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Common.Lube;

/// <summary>
/// Raised on someone trying to pick up a lubed item.
/// Cancelling will prevent the item slipping out of your hands.
/// </summary>
[ByRefEvent]
public record struct LubedPickUpAttemptEvent(bool Cancelled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.GLOVES;
}
