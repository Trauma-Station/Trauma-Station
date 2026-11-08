using Content.Shared.Hands.Components;
using Content.Shared.Inventory;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    private void TransferInventoryBehavior(EntityUid uid1, EntityUid uid2)
    {
        if (TryComp(uid1, out InventoryComponent? inventory1) && TryComp(uid2, out InventoryComponent? inventory2))
            _inventory.TransferEntityInventories((uid1, inventory1), (uid2, inventory2));
        else
            DropInventory(uid1);

        if (!TryComp(uid1, out HandsComponent? hands1) || !TryComp(uid2, out HandsComponent? hands2))
        {
            DropHeld(uid1);
            return;
        }

        foreach (var held in _hands.EnumerateHeld((uid1, hands1)))
        {
            _hands.TryDrop((uid1, hands1), held, checkActionBlocker: false);
            _hands.TryPickupAnyHand(uid2, held, handsComp: hands2);
        }
    }

    private void DropInventoryBehavior(EntityUid uid)
    {
        DropInventory(uid);
        DropHeld(uid);
    }

    private void DropInventory(EntityUid uid)
    {
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                _inventory.TryUnequip(uid, slot.ID, true, true);
            }
        }
    }

    private void DropHeld(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            _hands.TryDrop(uid, held);
        }
    }
}
