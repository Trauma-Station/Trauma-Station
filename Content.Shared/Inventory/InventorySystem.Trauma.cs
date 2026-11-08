using Content.Shared.Random;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

public partial class InventorySystem : EntitySystem
{
    [Dependency] private RandomHelperSystem _randomHelper = default!;

    /// <summary>
    /// Drop whatever item is in a given slot.
    /// Used by limb severing.
    /// </summary>
    public void DropSlotContents(Entity<InventoryComponent?> ent, [ForbidLiteral] ProtoId<InventorySlotPrototype> slotName)
    {
        if (!Resolve(ent, ref ent.Comp) || Transform(ent).MapID == MapId.Nullspace)
            return;

        if (TryUnequip(ent, slotName, out var item, silent: true, force: true, inventory: ent.Comp) &&
            !_containerSystem.IsEntityInContainer(item.Value))
            _randomHelper.RandomOffset(item.Value, 0.5f);
    }
}
