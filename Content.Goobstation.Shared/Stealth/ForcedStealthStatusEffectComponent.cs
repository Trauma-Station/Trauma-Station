// SPDX-License-Identifier: AGPL-3.0-or-later


// god the name
namespace Content.Goobstation.Shared.Stealth;

[RegisterComponent, NetworkedComponent]
public sealed partial class ForcedStealthStatusEffectComponent : Component
{
    [DataField]
    public float Visibility;

    [DataField]
    public bool RevealOnAttack = true;

    [DataField]
    public bool RevealOnDamage = true;

    /// <summary>
    /// Null if the target entity wasn't stealthed beforehand.
    /// </summary>
    [DataField]
    public float? OldVisibility;
}
