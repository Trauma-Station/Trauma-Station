// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawn any items from an entity table and try to equip or pick up them to the target entity.
/// </summary>
public sealed partial class EquipEntityTable : EntityEffectBase<EquipEntityTable>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public bool Silent;
}

public sealed partial class EquipEntityTableSystem : EntityEffectSystem<InventoryComponent, EquipEntityTable>
{
    [Dependency] private EntityTableSystem _table = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(Entity<InventoryComponent> ent, ref EntityEffectEvent<EquipEntityTable> args)
    {
        var e = args.Effect;
        var coords = Transform(ent).Coordinates;
        foreach (var id in _table.GetSpawns(e.Table))
        {
            var item = PredictedSpawnAtPosition(id, coords);
            if (!_inventory.TryEquip(ent, item, e.Slot, silent: e.Silent, predicted: true, inventory: ent.Comp))
                _hands.TryPickupAnyHand(ent.Owner, item);
        }
    }
}
