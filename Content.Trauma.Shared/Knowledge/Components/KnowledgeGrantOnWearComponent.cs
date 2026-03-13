// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Grants some knowledge when used in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class KnowledgeGrantOnWearComponent : Component
{
    /// <summary>
    /// Optional entity conditions checked against the wearer.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// Whether the knowledge was applied, if <see cref="Conditions"/> succeeded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Applied;

    /// <summary>
    /// Skills that will be added or boosted upon use.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> Skills = new();

    /// <summary>
    /// Experience that will be added per use.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> Experience = new();

    /// <summary>
    /// Can use art with this item?
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, bool> Blocked = new();

}
