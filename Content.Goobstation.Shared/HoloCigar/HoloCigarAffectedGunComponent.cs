// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
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
