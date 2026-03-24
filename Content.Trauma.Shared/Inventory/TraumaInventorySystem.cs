// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Trauma.Shared.Overlays;
using Content.Trauma.Shared.Tackle;

namespace Content.Trauma.Shared.Inventory;

public sealed class TraumaInventorySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, TackleEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowSquadIconsComponent>>(_inventory.RelayEvent); // Corvax-SecApartment
    }
}
