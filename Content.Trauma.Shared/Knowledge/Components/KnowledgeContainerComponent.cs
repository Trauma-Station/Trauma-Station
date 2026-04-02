// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Contains knowledge entities inside with <see cref="KnowledgeComponent"/>.
/// Assigned to some physical bodies, for example brains.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class KnowledgeContainerComponent : Component
{
    public const string ContainerId = "knowledge";

    /// <summary>
    /// The actual container that contains all knowledge entities.
    /// </summary>
    [ViewVariables]
    public Container? Container;

    /// <summary>
    /// The knowledge holder using this container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Holder;

    /// <summary>
    /// Contains a dictionary of prototypes to knowledge entities, which are stored inside <see cref="KnowledgeContainer"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> KnowledgeDict = new();

    /// <summary>
    /// The currently spoken language.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveLanguage;

    /// <summary>
    /// The currently enabled martial art.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveMartialArt;
}
