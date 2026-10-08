// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Common.Inventory;

/// <summary>
/// Prototype that every valid inventory slot has.
/// Used for validation in starting gear and what not.
/// </summary>
[Prototype]
public sealed partial class InventorySlotPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Slot flags that correspond to this slot.
    /// </summary>
    [DataField]
    public SlotFlags Flags = SlotFlags.NONE;
}
