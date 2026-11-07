// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Trauma.Server.Weapons.Melee;

public sealed partial class BatonDrainSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ItemToggleRequiresChargeComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out _, out var battery))
        {
            if (!_toggle.IsActivated(uid))
                continue;

            _battery.UseCharge((uid, battery), 5f * frameTime);

            if (_battery.GetCharge((uid, battery)) <= 0)
                _toggle.TryDeactivate(uid);
        }
    }
}
