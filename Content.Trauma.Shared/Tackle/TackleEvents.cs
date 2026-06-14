// SPDX-License-Identifier: AGPL-3.0-or-later

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

[ByRefEvent]
public record struct CalculateTackleModifierEvent(float Modifier = 0f, bool CanTackle = true) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
