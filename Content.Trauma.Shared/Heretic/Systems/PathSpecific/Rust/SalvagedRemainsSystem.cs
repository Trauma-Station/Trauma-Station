// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Tackle;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Rust;

public sealed partial class SalvagedRemainsSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedHereticAbilitySystem _ability = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;

    [Dependency] private EntityQuery<SalvagedRemainsComponent> _remainsQuery = default!;
    [Dependency] private EntityQuery<ToggleableClothingComponent> _toggleableQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<SalvagedRemainsComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SalvagedRemainsComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);

        Subs.SubscribeWithRelay<SalvagedRemainsComponent, CalculateTackleModifierEvent>(OnCalculateTackleModifier,
            baseEvent: false,
            held: false);
    }

    private void OnCalculateTackleModifier(Entity<SalvagedRemainsComponent> ent, ref CalculateTackleModifierEvent args)
    {
        if (!_toggle.IsActivated(ent.Owner))
            return;

        args.CanTackle = false;
    }

    private void OnGotUnequipped(Entity<SalvagedRemainsComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        Toggle(ent, false, args.Wearer);
    }

    private void OnGotEquipped(Entity<SalvagedRemainsComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Wearer))
            return;

        Toggle(ent, _ability.IsOnRust(args.Wearer), args.Wearer);
    }

    private void OnMove(Entity<InventoryComponent> ent, ref MoveEvent args)
    {
        if (args.OldPosition == args.NewPosition)
            return;

        if (!_inventory.TryGetSlotEntity(ent, "outerClothing", out var uid, ent.Comp) ||
            !_remainsQuery.HasComp(uid.Value) || !_heretic.IsHereticOrGhoul(ent))
            return;

        Toggle(uid.Value, _ability.IsOnRust(ent), ent);
    }

    private void Toggle(EntityUid uid, bool state, EntityUid user)
    {
        _toggle.TrySetActive(uid, state, user);

        if (!_toggleableQuery.TryComp(uid, out var toggleable))
            return;

        foreach (var clothingUid in toggleable.ClothingUids.Keys)
        {
            _toggle.TrySetActive(clothingUid, state, user);
        }
    }
}
