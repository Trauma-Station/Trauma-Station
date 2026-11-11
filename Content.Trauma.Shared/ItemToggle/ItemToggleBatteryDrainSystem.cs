// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Trauma.Shared.ItemToggle;

/// <summary>
/// Drains the battery of toggled items with <see cref="ItemToggleBatteryDrainComponent"/>
/// and turns them off when empty.
/// </summary>
public sealed partial class ItemToggleBatteryDrainSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    /// <summary>
    /// Applies <see cref="DrawRate"/> as a negative charge rate while the item is on.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRefreshChargeRate(Entity<ItemToggleBatteryDrainComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
            args.NewChargeRate -= ent.Comp.DrawRate;
    }

    /// <summary>
    /// Recalculates the charge rate when the item is toggled so the drain starts/stops immediately.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnToggled(Entity<ItemToggleBatteryDrainComponent> ent, ref ItemToggledEvent args)
    {
        _battery.RefreshChargeRate(ent.Owner);
    }

    /// <summary>
    /// Turns the item off when its battery reaches <see cref="BatteryState.Empty"/>.
    /// Needed because <see cref="ChargeChangedEvent"/> isn't reliably raised during passive rate drain.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBatteryStateChanged(Entity<ItemToggleBatteryDrainComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState == BatteryState.Empty)
            _toggle.TryDeactivate(ent.Owner);
    }
}
