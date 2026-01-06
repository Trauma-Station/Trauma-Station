// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityConditions;

/// <summary>
/// Condition that checks if the number of reagents in the target entity's bloodstream is within an exclusive range.
/// </summary>
public sealed partial class BloodstreamCountCondition : EntityConditionBase<BloodstreamCountCondition>
{
    [DataField]
    public int Max = int.MaxValue;

    [DataField]
    public int Min = -1;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("reagent-effect-condition-guidebook-unique-bloodstream-chem-threshold",
            ("max", Max),
            ("min", Min));
}

public sealed class BloodstreamCountConditionSystem : EntityConditionSystem<BloodstreamComponent, BloodstreamCountCondition>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    protected override void Condition(Entity<BloodstreamComponent> ent, ref EntityConditionEvent<BloodstreamCountCondition> args)
    {
        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var solution))
            return;

        // ignore the blood's expected reagents
        int count = 0;
        var blood = ent.Comp.BloodReferenceSolution;
        foreach (var quantity in solution.Contents)
        {
            var id = quantity.Reagent.Prototype;
            if (!blood.ContainsPrototype(id))
                count++;
        }
        var min = args.Condition.Min;
        var max = args.Condition.Max;
        args.Result = count > min && count < max;
    }
}
