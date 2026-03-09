// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class UpgradeableWeaponComponent : Component
{
    /// <summary>
    /// If specified, upgrades that support capacity will block any new upgrades from being inserted
    /// </summary>
    [DataField]
    public int? MaxUpgradeCapacity;
}
