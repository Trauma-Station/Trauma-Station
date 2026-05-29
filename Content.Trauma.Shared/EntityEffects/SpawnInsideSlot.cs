// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns entities inside an entity's inventory slot's storage.
/// Drops it next to them if that fails.
/// </summary>
public sealed partial class SpawnInsideSlot : BaseSpawnEntityEntityEffect<SpawnInsideSlot>
{
    /// <summary>
    /// The inventory slot to check for a bag etc.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;
}

public sealed partial class SpawnInsideSlotEntityEffectSystem : EntityEffectSystem<InventoryComponent, SpawnInsideSlot>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStorageSystem _storage = default!;

    protected override void Effect(Entity<InventoryComponent> ent, ref EntityEffectEvent<SpawnInsideSlot> args)
    {
        if (!args.Effect.Predicted && _net.IsClient)
            return;

        var quantity = args.Effect.Number * (int) Math.Floor(args.Scale);
        var slotId = args.Effect.Slot;
        var proto = args.Effect.Entity;
        var coords = Transform(ent).Coordinates;

        var target = ent.Owner;
        if (_inventory.TryGetSlotContainer(ent, slotId, out var slot, out _, inventory: ent.Comp) && slot.ContainedEntity is {} item)
            target = item;
        var storage = CompOrNull<StorageComponent>(target);

        for (var i = 0; i < quantity; i++)
        {
            var spawned = PredictedSpawnAtPosition(proto, coords);
            if (storage != null)
                _storage.Insert(target, spawned, out _, storageComp: storage, playSound: false);
        }
    }
}
