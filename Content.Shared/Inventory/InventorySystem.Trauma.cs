using Content.Shared.Random;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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

        foreach (var slot in ent.Comp.Slots)
        {
            if (slot.Name != slotName)
                continue;

            if (!TryGetSlotContainer(ent, slotName, out var container, out _, ent.Comp) ||
                container.ContainedEntity is not { } item ||
                !_containerSystem.Remove(item, container))
                break;

            _transform.AttachToGridOrMap(item);
            _randomHelper.RandomOffset(item, 0.5f);
            return;
        }
    }
}
