using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Grants some knowledge when used in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KnowledgeGrantOnWearComponent : Component
{
    /// <summary>
    /// Experience that will be added per use.
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> Experience = new();
}
