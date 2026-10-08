// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Trigger;
using Content.Trauma.Shared.Trigger.Conditions;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Trigger.Systems;

public sealed partial class TriggerConditionInSlotSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    [SubscribeLocalEvent]
    private void OnTrigger(Entity<TriggerConditionInSlotComponent> ent, ref TriggerEvent args)
    {
        if (!_inventory.TryGetContainingSlot((ent.Owner, null, null), out var slot) || slot.Name != ent.Comp.Slot)
            return;

        if (_containers.TryGetContainingContainer(ent.Owner, out var container))
            args.User = container.Owner;
    }
}
