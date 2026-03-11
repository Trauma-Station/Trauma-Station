// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Grants knowledge to the entity automatically on mapinit, then removes itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KnowledgeGrantComponent : Component
{
    /// <summary>
    /// Knowledge that will be added.
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public Dictionary<EntProtoId, int> Skills = new();
}
