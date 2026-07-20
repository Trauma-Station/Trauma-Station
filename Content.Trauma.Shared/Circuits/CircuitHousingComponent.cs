// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Circuit housing that adds/removes signal ports for its inserted <see cref="CircuitComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CircuitHousingSystem))]
[AutoGenerateComponentState]
public sealed partial class CircuitHousingComponent : Component
{
    /// <summary>
    /// The item slot to find a circuit in.
    /// </summary>
    [DataField]
    public string SlotId = "circuit";

    /// <summary>
    /// The installed circuit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Circuit;

    /// <summary>
    /// Set to true while receiving power from an APC or a powercell.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Powered;
}
