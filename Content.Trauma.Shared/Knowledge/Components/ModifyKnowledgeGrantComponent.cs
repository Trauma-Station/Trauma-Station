// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Adds to the skills of <see cref="KnowledgeGrantOnWearComponent"/> on mapinit then removes itself.
/// If this entity has a knowledge container it will also be applied immediately.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedKnowledgeSystem))]
public sealed partial class ModifyKnowledgeGrantComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Skills = new();
}
