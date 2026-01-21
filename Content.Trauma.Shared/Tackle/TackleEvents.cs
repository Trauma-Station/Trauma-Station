using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Tackle;

public sealed class TackleEvent(float range, float speed, float minDistance, TimeSpan knockdownTime)
    : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.GLOVES;

    public float Range = range;

    public float Speed = speed;

    public float MinDistance = minDistance;

    public TimeSpan KnockdownTime = knockdownTime;
}
