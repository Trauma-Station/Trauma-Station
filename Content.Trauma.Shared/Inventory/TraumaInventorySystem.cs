// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.Inventory;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Tackle;

namespace Content.Trauma.Shared.Inventory;

public sealed class TraumaInventorySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, TackleEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CheckMagicItemEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, TransformSpeakerJobIconEvent>(_inventory.RelayEvent);
    }
}
