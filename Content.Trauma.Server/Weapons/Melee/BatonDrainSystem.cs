// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Trauma.Server.Weapons.Melee;

public sealed partial class BatonDrainSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    private const float DrawRate = 5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleRequiresChargeComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<ItemToggleRequiresChargeComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ItemToggleRequiresChargeComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
    }

    private void OnRefreshChargeRate(Entity<ItemToggleRequiresChargeComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
            args.NewChargeRate -= DrawRate;
    }

    private void OnToggled(Entity<ItemToggleRequiresChargeComponent> ent, ref ItemToggledEvent args)
    {
        _battery.RefreshChargeRate(ent.Owner);
    }

    private void OnBatteryStateChanged(Entity<ItemToggleRequiresChargeComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState == BatteryState.Empty)
            _toggle.TryDeactivate(ent.Owner);
    }
}
