// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Mutation component that makes it remove some mutations when this is mutated.
/// Does nothing when this gets removed, or if the mutations weren't present.
/// </summary>
[RegisterComponent, Access(typeof(RemovesMutationSystem))]
public sealed partial class RemovesMutationComponent : Component
{
    /// <summary>
    /// The mutations to remove.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<MutationComponent>> Removes = new();
}
