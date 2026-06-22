// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Weapons.FoldingWeapon;

/// <summary>
/// Prevents shooting and wielding the weapon if it is toggled off
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FoldingWeaponComponent : Component
{
    [DataField]
    public bool SetPrefix = true;
}
