using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
/// Trauma - store knowledge profile for a species
/// </summary>
public sealed partial class SpeciesPrototype
{
    [DataField]
    public ProtoId<KnowledgeProfilePrototype> Knowledge = "Human";
}
