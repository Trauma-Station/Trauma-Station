// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Inventory;

/// <summary>
/// Raised on item before it gets transferred to
/// </summary>
[ByRefEvent]
public record struct InventoryItemTransferEvent(EntityUid Source, EntityUid Target, ProtoId<InventorySlotPrototype> Slot, bool Handled = false);
