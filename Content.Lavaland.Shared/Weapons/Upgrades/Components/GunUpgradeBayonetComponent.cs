// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Lavaland.Shared.Weapons.Upgrades.Components;

/// <summary>
///     Component to indicate a valid bayonet for weapon attachment
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunUpgradeBayonetComponent : Component;
