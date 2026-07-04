using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.Strip;

public sealed partial class ThievingSystem
{
    [Dependency] private InventorySystem _inventory = default!;

    private static readonly ProtoId<InventorySlotPrototype> GlovesSlot = "gloves";

    /// <summary>
    /// Returns whether the user currently has stealthy thieving gloves equipped.
    /// </summary>
    public bool IsStealthy(EntityUid user)
    {
        return _inventory.TryGetSlotEntity(user, GlovesSlot, out var gloves)
               && TryComp<ThievingComponent>(gloves, out var thieving)
               && thieving.Stealthy;
    }
}