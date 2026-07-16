using Content.Trauma.Common.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

public sealed partial class HideLayerClothingComponent
{
    /// <summary>
    /// EE Plasmeme Change: The clothing layers to hide.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<InventorySlotPrototype>>? ClothingSlots = new();
}
