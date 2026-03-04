using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Prototypes;

public sealed partial class ConstructionPrototype
{
    /// <summary>
    /// Construction Knowledge and levels that are required to be able to use this craft.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Groups = new();

    /// <summary>
    /// Coefficient that determines the quality coefficients on the component. Higher is better, and vice versa. Do not go above or below 0.5 and 2.
    /// </summary>
    [DataField]
    public float QualityCoefficient = 1.3f;
}
