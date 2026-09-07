using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Trauma.Common.TileMovement;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private EntityQuery<TileMovementComponent> _tileQuery = default!;

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
