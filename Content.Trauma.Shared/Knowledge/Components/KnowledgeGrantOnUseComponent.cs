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
    /// Length of a single doafter to learn this knowledge.
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If true, you will instantly gain all the skills then the item is ashed.
    /// Only <see cref="Skills"/> is used, <see cref="Experience"/> is ignored.
    /// </summary>
    [DataField]
    public bool SingleUse = true;

    /// <summary>
    /// Ash to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Ash = "Ash";
}
