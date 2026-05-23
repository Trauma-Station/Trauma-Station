// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Weapons.Ranged.Components;

public sealed partial class GunComponent : Component
{
    /// <summary>
    /// If true, the gun's accuracy is not affected by the user's shooting skill.
    /// </summary>
    [DataField]
    public bool UnaffectedBySkill;
}
