// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that succeeds if any equipped inventory item passes a nested condition.
/// </summary>
public sealed partial class InventoryAnyNestedCondition : EntityConditionBase<InventoryAnyNestedCondition>
{
    /// <summary>
    /// The nested condition to check inventory items against.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    [DataField]
    public LocId GuidebookText = "entity-condition-guidebook-inventory-nested";

    [DataField]
    public SlotFlags Flags = SlotFlags.WITHOUT_POCKET;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString(GuidebookText, ("condition", Condition.EntityConditionGuidebookText(prototype)));
}

public sealed class InventoryAnyNestedConditionSystem : EntityConditionSystem<InventoryComponent, InventoryAnyNestedCondition>
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;

    protected override void Condition(Entity<InventoryComponent> ent, ref EntityConditionEvent<InventoryAnyNestedCondition> args)
    {
        var flags = args.Condition.Flags;
        if (!_inventory.TryGetContainerSlotEnumerator((ent, ent), out var slots, flags))
            return;

        var condition = args.Condition.Condition;
        while (slots.NextItem(out var item))
        {
            if (_conditions.TryCondition(item, condition))
            {
                args.Result = true;
                return;
            }
        }
    }
}
