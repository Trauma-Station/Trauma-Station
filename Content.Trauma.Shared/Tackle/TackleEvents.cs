using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Tackle;

[ByRefEvent]
public record struct TackleEvent(
    float Range,
    float Speed,
    float StaminaCost,
    TimeSpan KnockdownTime,
    EntityUid User,
    EntityUid? Source = null) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.GLOVES;
}
