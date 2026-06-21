// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear.Reactor;

/// <summary>
/// Component for nuclear reactor control rods.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ReactorControlRodComponent : Component
{
    /// <summary>
    /// The target insertion level of the control rod.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ConfiguredInsertionLevel = 2;
}
