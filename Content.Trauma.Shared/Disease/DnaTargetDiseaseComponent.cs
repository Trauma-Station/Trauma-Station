// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Disease;

/// <summary>
/// Component for disease entities that stores a list of DNA strings that can be used with <see cref="DiseaseTargetDnaConditionComponent"/>.
/// Can be programmed with a syndicate dna sampler.
/// Dna-targeting effects will be disabled for non-target individuals to let it be transmitted.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DnaTargetDiseaseSystem))]
[AutoGenerateComponentState]
public sealed partial class DnaTargetDiseaseComponent : Component
{
    /// <summary>
    /// DNAs to check the host for.
    /// </summary>
    [DataField]
    public HashSet<string> TargetDnas = new();

    /// <summary>
    /// Whether dna-targeting effects are enabled for this host.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;
}
