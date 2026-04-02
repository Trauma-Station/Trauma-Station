// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for adding new components to projectiles shot by this weapon.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeProjectileComponentsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
