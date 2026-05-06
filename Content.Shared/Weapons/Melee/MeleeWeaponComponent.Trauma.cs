// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeWeaponComponent
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

    /// <summary>
    /// If true, alt attack will use click rather than wide swing
    /// </summary>
    [DataField]
    public bool AltClickAttack;

    /// <summary>
    /// Whitelist for the entities that can be hit on click attacks
    /// </summary>
    [DataField]
    public EntityWhitelist? ClickAttackWhitelist;

    /// <summary>
    /// If <see cref="AltClickAttack"/> is true, this acts as whitelist for entities that can be hit on click alt
    /// attacks
    /// </summary>
    [DataField]
    public EntityWhitelist? AltClickAttackWhitelist;

    /// <summary>
    /// If true, melee weapon won't miss on click attacks
    /// </summary>
    [DataField]
    public bool CanMiss = true;
}
