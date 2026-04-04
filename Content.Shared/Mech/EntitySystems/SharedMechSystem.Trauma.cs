using Content.Trauma.Common.Mech;
using Content.Trauma.Common.TileMovement;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly EntityQuery<TileMovementComponent> _tileQuery = default!;

    private void InitializeTrauma()
    {
        SubscribeLocalEvent<MechEquipmentComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<MechPilotComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
        SubscribeLocalEvent<MechComponent, GotEmaggedEvent>(OnEmagged);
    }

    // TODO: this has no reason to be here
    private void OnShotAttempted(EntityUid uid, MechEquipmentComponent component, ref ShotAttemptedEvent args)
    {
        if ((component.EquipmentOwner is not {} mech ||
            !HasComp<MechComponent>(mech)))
        {
            args.Cancel();
            return;
        }

        // TODO: this should not be in an attempt event
        var ev = new MechGunFiredEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    // TODO: this has no reason to be here
    private void OnEntGotRemovedFromContainer(EntityUid uid, MechPilotComponent component, EntGotRemovedFromContainerMessage args)
    {
        // Fixes scram implants or teleports locking the pilot out of being able to move.
        TryEject(component.Mech, pilot: uid);
    }

    // TODO: this has no reason to be here
    private void OnEmagged(EntityUid uid, MechComponent component, ref GotEmaggedEvent args)
    {
        if (!component.BreakOnEmag || !_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;
        args.Handled = true;
        component.EquipmentWhitelist = null;
        Dirty(uid, component);
    }

    private void CopyTileMovement(EntityUid mech, EntityUid pilot)
    {
        if (!_tileQuery.HasComp(pilot))
            return;

        var tile = EnsureComp<TileMovementComponent>(mech);
        tile.FromMech = true;
        Dirty(mech, tile);
    }

    private void ResetTileMovement(EntityUid mech)
    {
        if (_tileQuery.TryComp(mech, out var tile) && tile.FromMech)
            RemComp(mech, tile);
    }

    private void BlockHands(Entity<HandsComponent?> ent, EntityUid mech)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var freeHands = 0;
        foreach (var hand in _hands.EnumerateHands(ent))
        {
            if (!_hands.TryGetHeldItem(ent, hand, out var held))
            {
                freeHands++;
                continue;
            }

            // Is this entity removable? (they might have handcuffs on)
            if (HasComp<UnremoveableComponent>(held) && held != mech)
                continue;

            _hands.DoDrop(ent, hand);
            freeHands++;
            if (freeHands == 2)
                break;
        }
        if (_virtualItem.TrySpawnVirtualItemInHand(mech, ent.Owner, out var virtItem1))
            EnsureComp<UnremoveableComponent>(virtItem1.Value);

        if (_virtualItem.TrySpawnVirtualItemInHand(mech, ent.Owner, out var virtItem2))
            EnsureComp<UnremoveableComponent>(virtItem2.Value);
    }

    private void FreeHands(EntityUid uid, EntityUid mech)
    {
        _virtualItem.DeleteInHandsMatching(uid, mech);
    }
}
