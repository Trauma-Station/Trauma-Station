// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Console;

/// <summary>
/// A disk storing genetics data.
/// It can store either a mutation or unique enzymes, but not both.
/// This is for the geneticist what an id card is for HoP.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsDiskSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GeneticsDiskComponent : Component
{
    /// <summary>
    /// The mutation stored on this disk.
    /// It can be set by a genetics console while a mutated mob is in the scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<MutationComponent>? Mutation;

    /// <summary>
    /// The unique enzymes stored on this disk.
    /// It can be set by a genetics console while a scanned mutatable mob is in the scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public UniqueEnzymes? Enzymes;
}
