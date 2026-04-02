// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

[Access(typeof(GunUpgradeSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class GunUpgradeComponent : Component
{
    /// <summary>
    /// Literal name of this upgrade that is shown on all examine texts.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Text to use when examining the upgrade itself.
    /// </summary>
    [DataField]
    public LocId? ExamineTextType = "gun-upgrade-examine-type-upgrade";

    /// <summary>
    /// Text template to use when examining a weapon where this upgrade is inserted to.
    /// </summary>
    [DataField]
    public LocId? InsertedTextType = "gun-upgrade-inserted-examine-type-contains";

    [DataField]
    public int? CapacityCost;

    /// <summary>
    /// If this string matches with some other weapon upgrade, it will fil to install.
    /// </summary>
    [DataField]
    public string? UniqueGroup;
}
