// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Adds or removes tags to the target mob.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TagMutationComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> Added = new();

    [DataField]
    public List<ProtoId<TagPrototype>> Removed = new();
}
