// SPDX-FileCopyrightText: 2024 Centronias <me@centronias.com>
// SPDX-FileCopyrightText: 2024 SlamBamActionman <83650252+SlamBamActionman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityConditions;

public sealed partial class StaminaDamageCondition : EntityConditionBase<StaminaDamageCondition>
{
    [DataField]
    public float Min;

    [DataField]
    public float Max = float.PositiveInfinity;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("reagent-effect-condition-guidebook-stamina-damage-threshold",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
}

public sealed class StaminaDamageConditionSystem : EntityConditionSystem<StaminaComponent, StaminaDamageCondition>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void Condition(Entity<StaminaComponent> ent, ref EntityConditionEvent<StaminaDamageCondition> args)
    {
        var total = ent.Comp.StaminaDamage;
        var min = args.Condition.Min;
        var max = args.Condition.Max;
        args.Result = total > min && total < max;
    }
}
