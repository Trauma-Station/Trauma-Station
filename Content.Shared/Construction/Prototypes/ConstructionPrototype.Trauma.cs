using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Prototypes;

public sealed partial class ConstructionPrototype
{
    /// <summary>
    /// Construction Knowledge and levels that are required to be able to use this craft.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Groups = new();
}
