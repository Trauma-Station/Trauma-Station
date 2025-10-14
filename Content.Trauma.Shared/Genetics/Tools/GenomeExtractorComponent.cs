using Content.Shared.Damage;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Tools;

/// <summary>
/// Samples active and dormant mutations from a living target mob when clicked.
/// Deals damage if it successfully extracts mutations.
/// The genome can then be deposited into a genetics computer.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GenomeExtractorComponent))]
[AutoGenerateComponentState]
public sealed partial class GenomeExtractorComponent : Component
{
    /// <summary>
    /// Damage dealt on successful extraction of mutations from a target.
    /// If any of this damage is present on the target, extraction will fail.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Cellular", 20 }
        }
    };

    /// <summary>
    /// How long it takes to extract mutations from a target.
    /// Gets doubled for self-extraction.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Mutations extracted and their difficulty.
    /// Extracted active mutations are much easier to work with than dormant ones.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<MutationComponent>, float> Mutations = new();

    [ViewVariables]
    public bool IsEmpty => Mutations.Count == 0;
}
