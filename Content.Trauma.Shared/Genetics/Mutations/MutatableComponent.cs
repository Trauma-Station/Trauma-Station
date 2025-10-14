using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Allows an entity to have mutations.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationSystem))]
[AutoGenerateComponentState]
public sealed partial class MutatableComponent : Component
{
    /// <summary>
    /// The name of the container that stores mutations.
    /// </summary>
    [DataField]
    public string ContainerId = "mutations";

    /// <summary>
    /// Currently active mutations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<MutationComponent>, EntityUid> Mutations = new();

    /// <summary>
    /// Dormant mutations that can be added with a Activator for no instability cost.
    /// They are also what go into a mob's sequenced mutations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<MutationComponent>> Dormant = new();

    /// <summary>
    /// Add these mutations on map init, with a chance from 0-1.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId<MutationComponent>, float> DefaultMutations = new();

    /// <summary>
    /// How much instability you have from mutations.
    /// Once this reaches <see cref="MaxInstability"/> it's joever.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TotalInstability;
}
