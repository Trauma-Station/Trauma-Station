// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Grants some knowledge when used in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KnowledgeGrantOnUseComponent : Component
{
    /// <summary>
    /// Knowledge cap that can be used.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> Skills = new();

    /// <summary>
    /// Experience that will be added per use.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> Experience = new();

    /// <summary>
    /// Length of a simple doafter to learn this knowledge.
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(2);
}
