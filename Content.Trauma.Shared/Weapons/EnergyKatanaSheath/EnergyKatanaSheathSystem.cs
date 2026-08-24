// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;

namespace Content.Trauma.Shared.Weapons.EnergyKatanaSheath;

public sealed partial class EnergyKatanaSheathSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<EnergyKatanaSheathComponent> ent, ref GotEquippedEvent args)
    {
        if (_slots.GetItemOrNull(ent.Owner, ent.Comp.Slot) is not { } katana)
            return;

        var ev = new BindItemEvent(katana);
        RaiseLocalEvent(args.EquipTarget, ref ev);
    }
}
