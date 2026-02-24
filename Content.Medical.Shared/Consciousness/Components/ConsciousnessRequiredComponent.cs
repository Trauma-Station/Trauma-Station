// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Consciousness;

/// <summary>
/// Component for the brain that provides consciousness.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsciousnessRequiredComponent : Component
{
    /// <summary>
    /// Identifier, basically. Must be unique.
    /// </summary>
    [AutoNetworkedField, DataField]
    public string Identifier = "requiredConsciousnessPart";

    /// <summary>
    /// Not having this part means death, or only unconsciousness.
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool CausesDeath = true;
}
