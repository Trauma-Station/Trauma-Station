// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._White.Blocking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Trauma - public API extension
/// </summary>
public abstract partial class SharedBatterySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!; // _Trauma

    /// <summary>
    /// Gets the battery for an entity either if it is a battery, or from its power cell if it has a slot.
    /// </summary>
    public Entity<BatteryComponent>? GetBattery(EntityUid uid)
    {
        if (TryComp<BatteryComponent>(uid, out var battery))
            return (uid, battery);

        // not a battery and no slot found
        if (!TryComp<PowerCellSlotComponent>(uid, out var slotComp) ||
            _slots.GetItemOrNull(uid, slotComp.CellSlotId) is not {} cell)
            return null;

        return GetBattery(cell);
    }

    public bool ShouldDisable(EntityUid uid)
        => HasComp<RechargeableBlockingComponent>(uid);
}
