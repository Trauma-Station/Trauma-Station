// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores information about the entity that holds knowledge units,
/// see <see cref="KnowledgeContainerComponent"/>, usually a brain.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnowledgeHolderComponent : Component
{
    /// <summary>
    /// Pointer to the actual entity with KnowledgeContainerComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? KnowledgeEntity;
}
