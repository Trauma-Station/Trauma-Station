// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Contains knowledge entities inside with <see cref="KnowledgeComponent"/>.
/// Assigned to some physical bodies, for example brains.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnowledgeContainerComponent : Component
{
    public const string ContainerId = "knowledge";

    /// <summary>
    /// Contains all knowledge entities.
    /// </summary>
    [ViewVariables]
    public Container? KnowledgeContainer;

    /// <summary>
    /// Contains a dictionary of prototypes to knowledge entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> KnowledgeContainerIDs = new();

    /// <summary>
    ///    The skill entity that links to the current Language.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LanguageSkillUid;

    /// <summary>
    ///    The skill entity that links to the current MartialArt.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MartialArtSkillUid;
}
