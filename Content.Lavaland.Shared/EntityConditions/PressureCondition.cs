// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.EntityConditions;

/// <summary>
/// Requires that the target's tile be within an inclusive pressure range in kPa.
/// </summary>
public sealed partial class PressureCondition : EntityConditionBase<PressureCondition>
{
    /// <summary>
    /// If true, succeeds regardless of pressure if the current map has LavalandMapComponent.
    /// </summary>
    [DataField]
    public bool WorksOnLavaland;

    [DataField(required: true)]
    public float Min;

    [DataField(required: true)]
    public float Max;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("reagent-effect-condition-pressure-threshold",
            ("min", Min),
            ("max", Max));
}
