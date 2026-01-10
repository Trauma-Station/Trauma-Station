using Content.Trauma.Common.Knowledge.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores information about the entity that holds knowledge units,
/// see <see cref="KnowledgeContainerComponent"/>, usually a brain.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnowledgeHolderComponent : Component
{
    /// <summary>
    /// Sprite to display in the character UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? KnowledgeEntity;
}
