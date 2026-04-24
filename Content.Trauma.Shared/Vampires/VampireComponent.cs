// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Component that stores usable blood and total blood of a vampire.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireComponent : Component
{
    /// <summary>
    ///   The blood we can use right now.
    ///
    ///   This is the blood that counts towards abilities that require it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UsableBlood;

    /// <summary>
    ///  The total blood we have reached.
    ///  When using an action, we may lose <see cref="UsableBlood"/>,
    ///  but this variable still stays at our highest amount of blood we have reached so far.
    ///
    ///  This is the variable that unlocks abilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TotalBlood;
}
