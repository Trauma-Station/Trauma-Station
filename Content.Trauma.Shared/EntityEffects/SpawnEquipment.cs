// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Trauma.Common.Inventory;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns some items into the target's inventory, or drops them on the floor if they're full.
/// </summary>
public sealed partial class SpawnEquipment : EntityEffectBase<SpawnEquipment>
{
    /// <summary>
    /// Each inventory slot and item to spawn for it.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<InventorySlotPrototype>, EntProtoId> Slots;

    [DataField]
    public bool Predicted = true;
}

public sealed partial class SpawnEquipmentSystem : EntityEffectSystem<InventoryComponent, SpawnEquipment>
{
    [Dependency] private InventorySystem _inventory = default!;

    protected override void Effect(Entity<InventoryComponent> ent, ref EntityEffectEvent<SpawnEquipment> args)
    {
        var coords = Transform(ent).Coordinates;
        var predicted = args.Effect.Predicted;
        foreach (var (slot, id) in args.Effect.Slots)
        {
            var item = PredictedSpawnAtPosition(id, coords);
            _inventory.TryEquip(ent, item, slot, inventory: ent.Comp, predicted: predicted);
        }
    }
}
