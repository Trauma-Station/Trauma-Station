using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    /// <summary>
    /// Attempt to take an item from a slot if any are set to EjectOnInteract.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<ItemSlotsComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.EjectOnInteract || slot.Item == null || !CanEject(ent, slot, args.User, popup: args.User))
                continue;

            args.Handled = true;
            TryEjectToHands(ent, slot, args.User, true);
            break;
        }
    }

    /// <summary>
    /// Attempt to eject an item from the first valid item slot.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<ItemSlotsComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.EjectOnUse || slot.Item == null || !CanEject(ent, slot, args.User, popup: args.User))
                continue;

            args.Handled = true;
            TryEjectToHands(ent, slot, args.User, true);
            break;
        }
    }

    /// <summary>
    /// Tries to insert a held item into a fitting slot, swapping the current item when the selected slot allows it.
    /// </summary>
    /// <remarks>
    /// This only handles the event if the user has an applicable entity that can be inserted. This allows for
    /// other interactions to still happen (e.g., open UI, or toggle-open), despite the user holding an item.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<ItemSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryInsertWithConditions(ent, args.User, args.Used); // Trauma - use helper below
    }

    /// <summary>
    /// Trauma - helper moved out of OnInteractUsing.
    /// Returns true if it should handle interaction.
    /// </summary>
    public bool TryInsertWithConditions(Entity<ItemSlotsComponent> ent, EntityUid user, EntityUid used)
    {
        if (!TryComp(user, out HandsComponent? hands))
            return false;

        if (ent.Comp.Slots.Count == 0)
            return false;

        var slots = new List<ItemSlot>();
        string? whitelistFailPopup = null;
        string? lockedFailPopup = null;

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.InsertOnInteract)
                continue;

            if (CanInsert(ent, slot, used, user, slot.Swap))
            {
                slots.Add(slot);
            }
            else
            {
                var allowed = CanInsertWhitelist(used, slot);
                if (lockedFailPopup == null && slot.LockedFailPopup != null && allowed && slot.Locked)
                    lockedFailPopup = slot.LockedFailPopup;

                if (whitelistFailPopup == null && slot.WhitelistFailPopup != null && !allowed)
                    whitelistFailPopup = slot.WhitelistFailPopup;
            }
        }

        if (slots.Count == 0)
        {
            if (lockedFailPopup != null)
                _popupSystem.PopupEntity(Loc.GetString(lockedFailPopup), ent, user);
            else if (whitelistFailPopup != null)
                _popupSystem.PopupEntity(Loc.GetString(whitelistFailPopup), ent, user);
            return false;
        }

        if (!_handsSystem.TryDrop(user, used))
            return false;

        slots.Sort(SortEmpty);

        foreach (var slot in slots)
        {
            if (slot.Item != null)
                _handsSystem.TryPickupAnyHand(user, slot.Item.Value, handsComp: hands);

            if (!Insert(ent, slot, used, user, excludeUserAudio: true))
                return false;

            if (slot.InsertSuccessPopup.HasValue)
                _popupSystem.PopupEntity(Loc.GetString(slot.InsertSuccessPopup), ent, user);

            return true;
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void HandleButtonPressed(Entity<ItemSlotsComponent> ent, ref ItemSlotButtonPressedEvent args)
    {
        if (!ent.Comp.Slots.TryGetValue(args.SlotId, out var slot))
            return;

        if (args.TryEject && slot.HasItem && !slot.DisableEject)
            TryEjectToHands(ent, slot, args.Actor, true);
        else if (args.TryInsert && !slot.HasItem)
            TryInsertFromHand(ent, slot, args.Actor);
    }
}
