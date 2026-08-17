// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Trauma.Shared.EntityEffects.Botany;

/// <summary>
///     Handles increase or decrease of plant maturation.
/// </summary>
public sealed partial class PlantAdjustMaturation : BasePlantAdjustAttribute<PlantAdjustMaturation>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-maturation";
}
