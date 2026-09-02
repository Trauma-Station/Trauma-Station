using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Materials;
using Content.Shared.Strip;
using Robust.Shared.Collections;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ThievingSystem _thieving = default!;

    public bool TryInsertWithConditions(Entity<ItemSlotsComponent> ent, EntityUid user, EntityUid toInsert, bool doAfter = true)
    {
        if (!TryComp(user, out HandsComponent? hands))
            return false;

        if (ent.Comp.Slots.Count == 0)
            return false;

        // If any slot can be inserted into don't show popup.
        // If any whitelist passes, but slot is locked, then show locked.
        // If whitelist fails all, show whitelist fail.

        // valid, insertable slots (if any)
        var slots = new ValueList<ItemSlot>();

        string? whitelistFailPopup = null;
        string? lockedFailPopup = null;
        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.InsertOnInteract)
                continue;

            if (CanInsert(ent, slot, toInsert, user, slot.Swap))
            {
                slots.Add(slot);
                break; //Goobstation: If an item has multiple ItemSlots, stick with the highest priority and stop looking.
            }
            else
            {
                var allowed = CanInsertWhitelist(toInsert, slot);
                if (lockedFailPopup == null && slot.LockedFailPopup != null && allowed && slot.Locked)
                    lockedFailPopup = slot.LockedFailPopup;

                if (whitelistFailPopup == null && slot.WhitelistFailPopup != null)
                    whitelistFailPopup = slot.WhitelistFailPopup;
            }
        }

        if (slots.Count == 0)
        {
            // it's a bit weird that the popupMessage is stored with the item slots themselves, but in practice
            // the popup messages will just all be the same, so it's probably fine.
            //
            // doing a check to make sure that they're all the same or something is probably frivolous
            if (lockedFailPopup != null)
                _popupSystem.PopupClient(Loc.GetString(lockedFailPopup), ent, user);
            else if (whitelistFailPopup != null)
                _popupSystem.PopupClient(Loc.GetString(whitelistFailPopup), ent, user);
            return false;
        }
        slots.Sort(SortEmpty);

        foreach (var slot in slots)
        {
            if (TryInsertOrDoAfter(ent, slot, toInsert, (user, hands), doAfter))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to start a do-after if it can, otherwise
    /// </summary>
    public bool TryInsertOrDoAfter(Entity<ItemSlotsComponent> ent, ItemSlot slot, EntityUid toInsert, Entity<HandsComponent?> user, bool doAfter = true)
    {
        // Handle do-after insert
        if (doAfter && TryStartInsertDoAfter(slot, toInsert, user))
            return true; // We are delaying it to some time

        // Drop the held item onto the floor. Return if the user cannot drop.
        if (_handsSystem.IsHolding(user, toInsert) && !_handsSystem.TryDrop(user, toInsert))
            return false;

        if (slot.Item is { } item)
            _handsSystem.TryPickupAnyHand(user, item, handsComp: user.Comp);

        Insert(ent, slot, toInsert, user, excludeUserAudio: true);

        if (slot.InsertSuccessPopup.HasValue)
            _popupSystem.PopupClient(Loc.GetString(slot.InsertSuccessPopup), ent, user);
        return true;
    }

    private bool TryStartInsertDoAfter(ItemSlot slot, EntityUid item, EntityUid? user)
    {
        if (slot.InsertDelay is not {} delay || user == null)
            return false;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user.Value,
            delay,
            new ItemSlotInteractionDoAfterEvent(slot.ID!, false, true),
            slot.ContainerSlot?.Owner,
            item,
            item)
        {
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnDamage = true,
        });
    }

    private bool TryStartEjectDoAfter(ItemSlot slot, EntityUid item, EntityUid? user)
    {
        if (slot.EjectDelay != null && user != null)
        {
            return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                user.Value,
                slot.EjectDelay.Value,
                new ItemSlotInteractionDoAfterEvent(slot.ID!, true, false),
                item)
            {
                BreakOnHandChange = true,
                BreakOnMove = true,
                BreakOnDropItem = true,
                BreakOnDamage = true,
            });
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnReclaimed(EntityUid uid, ItemSlotsComponent component, GotReclaimedEvent args)
    {
        foreach (var slot in component.Slots.Values)
        {
            if (slot.ContainerSlot != null)
                _containers.EmptyContainer(slot.ContainerSlot, destination: args.ReclaimerCoordinates);
        }
    }

    [SubscribeLocalEvent]
    private void HandleDoAfter(Entity<ItemSlotsComponent> ent, ref ItemSlotInteractionDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            !ent.Comp.Slots.TryGetValue(args.SlotId, out var slot))
            return;

        if (args.TryEject && slot.HasItem)
            TryEjectToHands(ent, slot, args.User, true, doAfter: false);
        else if (args.TryInsert && !slot.HasItem && args.Used is { } item)
            TryInsertWithConditions(ent, args.User, item, doAfter: false);
    }
}
