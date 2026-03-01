// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Weapons.Ranged.Ammo;

/// <summary>
/// Any projectile with this component will make bullet-holes when it hits another entity with the BulletholeComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BulletHoleGeneratorComponent : Component;
