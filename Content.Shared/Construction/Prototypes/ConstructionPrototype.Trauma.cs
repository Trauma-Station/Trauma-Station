using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Prototypes;

public sealed partial class ConstructionPrototype
{
    /// <summary>
    /// Knowledge masteries that are required to be able to make this craft.
    /// Mastery is from 0-5.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Theory = new();

    /// <summary>
    /// If non-null, the skills you need to make a normal quality item.
    /// This lets you make something easy to understand how to make but hard to do well.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int>? Practical;

    /// <summary>
    /// Optional quality override.
    /// </summary>
    [DataField]
    public float? QualityCoefficient;
}
