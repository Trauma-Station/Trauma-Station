// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Cybernetics;

/// <summary>
/// Component for cybernetic organs that can be installed in entities.
/// Disables the organ while EMP'd.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberneticsComponent : Component
{
    /// <summary>
    /// Is it currently disabled by EMPs?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Disabled;

    /// <summary>
    /// Damage to deal to the part containing this organ, or the part itself.
    /// </summary>
    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new()
        {
            { "Shock", 30 }
        }
    };
}
