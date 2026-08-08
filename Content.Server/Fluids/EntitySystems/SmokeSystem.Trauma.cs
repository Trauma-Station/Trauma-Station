using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class SmokeSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mob = default!:
}
