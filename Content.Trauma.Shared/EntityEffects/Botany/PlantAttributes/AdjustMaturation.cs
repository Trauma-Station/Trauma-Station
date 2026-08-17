using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Trauma.Shared.EntityEffects.Botany.PlantAttributes;

/// <summary>
///     Handles increase or decrease of plant maturation.
/// </summary>
public sealed partial class PlantAdjustMaturation : BasePlantAdjustAttribute<PlantAdjustMaturation>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-maturation";
}
