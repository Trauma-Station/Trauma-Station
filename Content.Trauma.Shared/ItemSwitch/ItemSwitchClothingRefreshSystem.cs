// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Trauma.Shared.ItemSwitch;

/// <summary>
/// Cleans up and re-grants clothing components when an ItemSwitch state changes while worn,
/// so ClothingGrantComponentComponent correctly updates the wearer
/// </summary>
public sealed partial class ItemSwitchClothingRefreshSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void OnItemSwitched(Entity<ItemSwitchClothingRefreshComponent> ent, ref ItemSwitchedEvent args)
    {
        if (args.User == null
            || !TryComp<InventoryComponent>(args.User.Value, out var inventory))
            return;

        var dirty = false;

        // Remove components granted by the previous state
        if (ent.Comp.Wearer != null)
        {
            foreach (var name in ent.Comp.GrantedComponents)
            {
                var type = Factory.GetRegistration(name).Type;
                RemComp(ent.Comp.Wearer.Value, type);
            }
            ent.Comp.GrantedComponents.Clear();
            ent.Comp.Wearer = null;
            dirty = true;
        }

        var enumerator = new InventorySystem.InventorySlotEnumerator(inventory, ent.Comp.TargetSlots);
        while (enumerator.NextItem(out var item, out var slotDef))
        {
            if (item != ent.Owner)
                continue;

            // Record components granted by the new state for later cleanup
            if (TryComp<ClothingGrantComponentComponent>(ent, out var grant))
            {
                foreach (var name in grant.Components.Keys)
                {
                    var type = Factory.GetRegistration(name).Type;
                    if (!HasComp(args.User.Value, type))
                        ent.Comp.GrantedComponents.Add(name);
                }
                ent.Comp.Wearer = args.User.Value;
                dirty = true;
            }

            var equipEv = new GotEquippedEvent(args.User.Value, ent.Owner, slotDef);
            RaiseLocalEvent(ent.Owner, equipEv, true);
            break;
        }

        if (dirty)
            Dirty(ent);
    }
}
