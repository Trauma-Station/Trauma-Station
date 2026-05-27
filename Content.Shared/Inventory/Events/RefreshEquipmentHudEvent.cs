namespace Content.Shared.Inventory.Events;

[ByRefEvent]
public record struct RefreshEquipmentHudEvent<T>(SlotFlags TargetSlots, EntityUid Uid, bool WorksInHands = false) : IInventoryRelayEvent // Trauma - added Uid and WorksInHands
    where T : IComponent
{
    public SlotFlags TargetSlots { get; } = TargetSlots;
    public bool Active = false;
    public List<T> Components = new();
}
