// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Knowledge.Skills.Components;

/// <summary>
/// Contains knowledge entities inside with <see cref="SkillComponent"/>.
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

    [DataField]
    public Dictionary<EntProtoId, SpecializationStats> WeaponSpecializations = new();
}

[DataDefinition]
public partial struct SpecializationStats
{
    [DataField] public int Attack;
    [DataField] public int Defense;
    [DataField] public int Speed;
}