// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 yglop <95057024+yglop@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Stacks;

namespace Content.Goobstation.Shared.MaterialEnergy;

public sealed class MaterialEnergySystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialEnergyComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, MaterialEnergyComponent component, InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<BatteryComponent>(uid, out var battery) ||
            !TryComp<PhysicalCompositionComponent>(args.Used, out var composition) ||
            !TryComp<StackComponent>(args.Used, out var stack))
            return;

        var ent = (uid, battery);
        var item = (args.Used, stack);
        foreach (var fueltype in component.MaterialWhiteList)
        {
            if (composition.MaterialComposition.TryGetValue(fueltype, out var amount))
            {
                AddBatteryCharge(
                    ent,
                    item,
                    amount);
            }
        }
    }

    private void AddBatteryCharge(
        Entity<BatteryComponent> cutter,
        Entity<StackComponent> material,
        int materialPerSheet)
    {
        var chargeDiff = (int) (cutter.Comp.MaxCharge - _battery.GetCharge(cutter.AsNullable()));
        if (chargeDiff <= 0)
            return;

        var sheets = material.Comp.Count;
        var totalMaterial = materialPerSheet * sheets;
        var materialLeft = totalMaterial - chargeDiff;
        var chargeToAdd = 0;

        if (materialLeft == 0)
        {
            chargeToAdd = totalMaterial;
        }
        else if (materialLeft > 0)
        {
            chargeToAdd = totalMaterial - materialLeft;
        }
        else
        {
            chargeToAdd = Math.Abs(Math.Abs(materialLeft) - chargeDiff);
        }

        _battery.ChangeCharge(cutter.AsNullable(), chargeToAdd);

        _stack.ReduceCount(material.AsNullable(), chargeToAdd / materialPerSheet);
    }
}
