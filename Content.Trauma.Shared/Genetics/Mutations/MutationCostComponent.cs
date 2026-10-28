// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Mutation component to change how much biomass it takes to work with.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationSystem))]
[EntityCategory("Mutations")]
public sealed partial class MutationCostComponent : Component
{
    /// <summary>
    /// Multiplier on cost to print mutators, activators or cleansers.
    /// </summary>
    [DataField(required: true)]
    public int BiomassMultiplier;
}
