using Content.Shared.Electrocution;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor; // great namespacing here guys
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Goobstation.Shared.SuitSensors;

public sealed class SuitSensorShockableSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSuitSensorSystem _suitSensor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, ElectrocutedEvent>(OnElectrocuted);
    }

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

            _suitSensor.SetSensor((item, sensor), _random.Pick(modes), ent);
            _popup.PopupEntity(Loc.GetString("suit-sensor-got-shocked", ("suit", item)),
                ent,
                ent,
                PopupType.MediumCaution);
        }
    }
}
