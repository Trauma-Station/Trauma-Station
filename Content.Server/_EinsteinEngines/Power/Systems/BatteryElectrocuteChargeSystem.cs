// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Electrocution;
using Content.Server.Power.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Electrocution;
using Robust.Shared.Random;

namespace Content.Server._EinsteinEngines.Power.Systems;

public sealed class BatteryElectrocuteChargeSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, ElectrocutedEvent>(OnElectrocuted);
    }

    private void OnElectrocuted(Entity<BatteryComponent> ent, ref ElectrocutedEvent args)
    {
        if (args.ShockDamage == null || args.ShockDamage <= 0)
            return;

        var charge = Math.Min(args.ShockDamage.Value * args.SiemensCoefficient
            / ElectrocutionSystem.ElectrifiedDamagePerWatt * 2,
                ent.Comp.MaxCharge * 0.25f)
            * _random.NextFloat(0.75f, 1.25f);

        _battery.ChangeCharge(ent.AsNullable(), charge);

        _popup.PopupEntity(Loc.GetString("battery-electrocute-charge"), ent, ent);
    }
}
