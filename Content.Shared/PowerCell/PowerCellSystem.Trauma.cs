// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Inventory;
using Content.Shared.Power.Components;

namespace Content.Shared.PowerCell;

/// <summary>
/// Trauma - provide <see cref="FindBattery"/> that uses <see cref="FindBatteryEvent"/>.
/// </summary>
public sealed partial class PowerCellSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private Entity<PredictedBatteryComponent>? FindBattery(EntityUid uid)
    {
        var ev = new FindBatteryEvent();
        RaiseLocalEvent(uid, ref ev);
        // only relay to equipment if no battery was found in the entity itself.
        if (ev.FoundBattery == null && TryComp<InventoryComponent>(uid, out var inventory))
            _inventory.RelayEvent((uid, inventory), ref ev);

        return ev.FoundBattery;
    }
}

/// <summary>
/// Raised on an entity to find a battery to charge etc.
/// It gets raised on the entity itself then, if no battery was found, relayed to equipped items.
/// </summary>
/// <remarks>
/// This can't be in common since it needs Content.Shared.Inventory and Content.Shared.Power.Components
/// </remarks>
[ByRefEvent]
public record struct FindBatteryEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public Entity<PredictedBatteryComponent>? FoundBattery;
}
