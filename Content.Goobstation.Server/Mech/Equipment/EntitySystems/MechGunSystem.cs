// SPDX-FileCopyrightText: 2024 NULL882 <gost6865@yandex.ru>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
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
