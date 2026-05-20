// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Systems;

namespace Content.Trauma.Shared.Weapons.EnergyKatanaSheath;

public sealed partial class EnergyKatanaSheathSystem : EntitySystem
{
    [Dependency] private SharedSpaceNinjaSystem _ninja = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyKatanaSheathComponent, GotEquippedEvent>(OnEquipped);
    }

    private void OnEquipped(Entity<EnergyKatanaSheathComponent> ent, ref GotEquippedEvent args)
    {
        if (_slots.GetItemOrNull(ent, ent.Comp.Slot) is not { } katana)
            return;

        _ninja.BindKatana(args.EquipTarget, katana);
    }
}
