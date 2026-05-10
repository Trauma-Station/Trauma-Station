// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Common.Glue;

/// <summary>
/// Raised on someone trying to pick up a glued item.
/// Cancelling will prevent the item becoming unremoveable.
/// </summary>
[ByRefEvent]
public record struct GluedPickUpAttemptEvent(bool Cancelled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.GLOVES;
}
