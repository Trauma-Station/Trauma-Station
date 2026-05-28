// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Goobstation.Shared.Chemistry;

[ByRefEvent]
public record struct VaporCheckEyeProtectionEvent : IInventoryRelayEvent
{
    public bool Protected;
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
}
