// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Ranged;

namespace Content.Trauma.Shared.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class MultiMagazineAmmoProviderComponent : MagazineAmmoProviderComponent
{
    /// <summary>
    /// Slots that this ammo provider uses
    /// slot id -> (null ? use mag's ammo provider projectile and values : use ammo provider values multiplied by float, don't add projectile)
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, float?> Slots;
}
