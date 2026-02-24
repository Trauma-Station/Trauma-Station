using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores information about a set of constructed object
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnowledgeConstructionModifierComponent : Component
{
    /// <summary>
    /// Stores the level mastery of the item required to modify it.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId, int> LevelDeltas = new();

    /// <summary>
    /// Stores the quality of the item, which changes some functionality when used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Quality = 0;
}
