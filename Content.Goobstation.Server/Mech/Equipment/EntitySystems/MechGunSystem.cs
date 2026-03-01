// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mech.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Power.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Goobstation.Server.Mech.Equipment.EntitySystems;

public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, HandleMechEquipmentBatteryEvent>(OnHandleMechEquipmentBattery);
    }

    private void OnHandleMechEquipmentBattery(EntityUid uid, MechEquipmentComponent component, HandleMechEquipmentBatteryEvent args)
    {
        if (component.EquipmentOwner == null || !TryComp<BatteryComponent>(uid, out var battery))
            return;

        var charge = _battery.GetCharge((uid, battery));
        if (TryComp<BatteryAmmoProviderComponent>(uid, out var ammo) && ammo.FireCost > charge)
            return;

        ChargeGunBattery(uid, battery, charge);
    }

    private void ChargeGunBattery(EntityUid uid, BatteryComponent component, float currentCharge)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var mechEquipment) || !mechEquipment.EquipmentOwner.HasValue)
            return;

        if (!TryComp<MechComponent>(mechEquipment.EquipmentOwner.Value, out var mech))
            return;

        var maxCharge = component.MaxCharge;

        var chargeDelta = maxCharge - currentCharge;

        // TODO: The battery charge of the mech would be spent directly when fired.
        if (chargeDelta <= 0 || mech.Energy - chargeDelta < 0)
            return;

        if (!_mech.TryChangeEnergy(mechEquipment.EquipmentOwner.Value, -chargeDelta, mech))
            return;

        _battery.SetCharge((uid, component), maxCharge);
    }
}

[ByRefEvent]
public record struct CheckMechWeaponBatteryEvent(BatteryComponent Battery, bool Cancelled = false);
