using Content.Shared.Inventory;
using Content.Trauma.Shared.Tackle;

namespace Content.Trauma.Shared.Inventory;

public sealed class TraumaInventorySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, TackleEvent>(RefRelayInventoryEvent);
    }


    private void RefRelayInventoryEvent<T>(EntityUid uid, InventoryComponent component, ref T args) where T : IInventoryRelayEvent
    {
        _inventory.RelayEvent((uid, component), ref args);
    }
}
