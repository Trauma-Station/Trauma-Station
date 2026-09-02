// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Goobstation.Shared.Flashbang;

[ByRefEvent]
public record struct GetFlashbangedEvent(float ProtectionRange) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EARS | SlotFlags.HEAD;
}
