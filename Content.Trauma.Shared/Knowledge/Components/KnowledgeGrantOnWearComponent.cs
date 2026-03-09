// SPDX-License-Identifier: AGPL-3.0-or-later

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
