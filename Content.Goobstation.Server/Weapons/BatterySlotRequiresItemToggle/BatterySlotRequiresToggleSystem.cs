// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Goobstation.Server.Weapons.BatterySlotRequiresItemToggle;

public sealed partial class BatterySlotRequiresToggleSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;

    [SubscribeLocalEvent]
    private void OnToggled(Entity<BatterySlotRequiresToggleComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots) ||
            !_slots.TryGetSlot((ent, slots), ent.Comp.ItemSlot, out var slot))
            return;

        _slots.SetLock((ent, slots), slot, args.Activated ^ ent.Comp.Inverted);
    }
}
