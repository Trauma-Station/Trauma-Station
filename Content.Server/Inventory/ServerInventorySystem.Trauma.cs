using Content.Shared.Inventory;

namespace Content.Server.Inventory;

public sealed partial class ServerInventorySystem
{
    // Save inventory items in a dictionary before transferring to not lose items in child slots like suitstorage, pda, pockets
    private readonly Dictionary<EntityUid, SlotDefinition> _toTransfer = new();
}
