// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Removes a set of skills when this one gets added.
/// This relationship is one-way, so add it to the other knowledges too if you want.
/// It also does not prevent learning these skills in the future.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(KnowledgeConflictSystem))]
public sealed partial class KnowledgeConflictComponent : Component
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Conflicts = new();
}
