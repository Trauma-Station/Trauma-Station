// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Inventory;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// For an entity with an inventory, applies an effect to all items in the inventory within specific <see cref="SlotFlags"/>.
/// </summary>
public sealed partial class RelayInventory : EntityEffectBase<RelayInventory>
{
    /// <summary>
    /// Effect to apply to the items.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    /// <summary>
    /// Which slot flags to look for.
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.NONE;
}

public sealed partial class RelayInventoryEffectSystem : EntityEffectSystem<InventoryComponent, RelayInventory>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private InventorySystem _inventory = default!;

    protected override void Effect(Entity<InventoryComponent> ent, ref EntityEffectEvent<RelayInventory> args)
    {
        var effect = args.Effect;

        _inventory.TryGetContainerSlotEnumerator(ent.AsNullable(), out var enumerator, effect.SlotFlags);
        while (enumerator.NextItem(out var item))
        {
            _data.CopyData(ent, item);
            _effects.TryApplyEffect(item, effect.Effect, args.Scale, args.User, predicted: args.Predicted);
            _data.ClearData(item);
        }
    }
}
