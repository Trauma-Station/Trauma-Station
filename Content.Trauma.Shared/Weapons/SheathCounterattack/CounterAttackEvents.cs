// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Weapons.SheathCounterattack;

[ByRefEvent]
public record struct GetCounterAttackSheathEvent(Entity<SheathCounterattackComponent>? Sheath = null) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.BELT;
}
