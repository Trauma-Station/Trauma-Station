// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.HoloCigar;

/// <summary>
/// This is used for tracking affected HoloCigar weapons.
/// </summary>
[RegisterComponent]
public sealed partial class HoloCigarAffectedGunComponent : Component
{
    [DataField]
    public EntityUid GunOwner;

    [DataField]
    public bool WasOriginallyMultishot;

    [DataField]
    public float OriginalMissChance;

    [DataField]
    public float OriginalSpreadModifier;

    [DataField]
    public float OriginalSpreadAddition;
}
