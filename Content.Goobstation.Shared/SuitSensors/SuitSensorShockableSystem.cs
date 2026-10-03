// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Electrocution;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.SuitSensors;

public sealed partial class SuitSensorShockableSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSuitSensorSystem _suitSensor = default!;

    [SubscribeLocalEvent]
    private void OnElectrocuted(Entity<InventoryComponent> ent, ref ElectrocutedEvent args)
    {
        var enumerator = _inventory.GetSlotEnumerator(ent.AsNullable());
        var modes = Enum.GetValues<SuitSensorMode>();

        while (enumerator.MoveNext(out var containerSlot))
        {
            if (containerSlot.ContainedEntity is not { } item
                || !HasComp<SuitSensorShockableComponent>(item)
                || !TryComp<SuitSensorComponent>(item, out var sensor)
                || sensor.ControlsLocked
                || sensor.User != ent.Owner)
                continue;

            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(item));
            _suitSensor.SetSensor((item, sensor), rand.Pick(modes), ent);
            _popup.PopupEntity(Loc.GetString("suit-sensor-got-shocked", ("suit", item)),
                ent,
                ent,
                PopupType.MediumCaution);
        }
    }
}
