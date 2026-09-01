// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Goobstation.Shared.Clothing;

[ByRefEvent]
public record struct DelayedKnockdownAttemptEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.OUTERCLOTHING;

    public TimeSpan DelayDelta;
    public TimeSpan KnockdownTimeDelta;
    public bool Cancelled;
}
