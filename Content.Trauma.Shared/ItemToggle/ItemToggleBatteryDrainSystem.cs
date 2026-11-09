// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Trauma.Shared.ItemToggle;

public sealed partial class ItemToggleBatteryDrainSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    private const float DrawRate = 5f;

    [SubscribeLocalEvent]
    private void OnRefreshChargeRate(Entity<ItemToggleRequiresChargeComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
            args.NewChargeRate -= DrawRate;
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<ItemToggleRequiresChargeComponent> ent, ref ItemToggledEvent args)
    {
        _battery.RefreshChargeRate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBatteryStateChanged(Entity<ItemToggleRequiresChargeComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState == BatteryState.Empty)
            _toggle.TryDeactivate(ent.Owner);
    }
}
