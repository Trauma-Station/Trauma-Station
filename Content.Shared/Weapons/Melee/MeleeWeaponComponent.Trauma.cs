// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeWeaponComponent : Component
{
    /// <summary>
    /// If false, light attacks by this weapon cannot be parried
    /// </summary>
    [DataField]
    public bool CanParryLight = true;

    /// <summary>
    /// If false, wide attacks by this weapon cannot be parried
    /// </summary>
    [DataField]
    public bool CanParryWide = true;
}
