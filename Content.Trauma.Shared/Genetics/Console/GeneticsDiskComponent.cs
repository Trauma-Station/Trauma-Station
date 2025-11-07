using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Console;

/// <summary>
/// A disk storing genetics data.
/// This is for the geneticist what an id card is for HoP.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsDiskSystem))]
[AutoGenerateComponentState]
public sealed partial class GeneticsDiskComponent : Component
{
    /// <summary>
    /// The mutation stored on this disk.
    /// It can be set by a genetics console while a mutated mob is in the scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<MutationComponent>? Mutation;
}
