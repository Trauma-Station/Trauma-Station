// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;

namespace Content.Goobstation.Shared.Factory.Slots;

/// <summary>
/// Abstraction over an <see cref="ItemSlot"/> on the machine.
/// </summary>
public sealed partial class AutomatedItemSlot : AutomationSlot
{
    /// <summary>
    /// The name of the slot to automate.
    /// </summary>
    [DataField(required: true)]
    public string SlotId = string.Empty;

    private ItemSlotsSystem _slots;

    private ItemSlot? _slot;

    [ViewVariables]
    public ItemSlot Slot
    {
        get
        {
            if (_slot is {} slot)
                return slot;

            if (_slots.TryGetSlot(Owner, SlotId, out _slot))
                return _slot;

            throw new InvalidOperationException($"Entity {EntMan.ToPrettyString(Owner)} had no item slot {SlotId}");
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _slots = EntMan.System<ItemSlotsSystem>();
    }

    public override bool Insert(EntityUid item)
        => base.Insert(item) &&
            _slots.TryInsert(Owner, Slot, item, user: null);

    public override bool CanInsert(EntityUid item)
        => base.CanInsert(item) &&
            _slots.CanInsert(Owner, Slot, item, user: null);

    public override EntityUid? GetItem(EntityUid? filter)
        => Slot.Item is not {} item || _filter.IsBlocked(filter, item)
            ? null
            : item;
}
