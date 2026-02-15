// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Body.Part;

/// <summary>
/// Enabled <c>NeedsHands</c> for the body's <c>Puller</c> when added, and disables it when removed.
/// Can be set to only work for species matching a whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PullerTailSystem))]
[AutoGenerateComponentState]
public sealed partial class PullerTailComponent : Component
{
    /// <summary>
    /// List of species that it works for.
    /// Human brain can't use a lizard tail effectively.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>>? SpeciesWhitelist;

    [DataField, AutoNetworkedField]
    public bool Changed;
}
