// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Tackle;

[ByRefEvent]
public record struct TackleEvent(EntityUid User, List<TackleModifier> Sources) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

[ByRefEvent]
public record struct CalculateTackleModifierEvent(float Modifier = 0f, bool CanTackle = true) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
