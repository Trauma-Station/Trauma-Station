// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.WeaponRandomExplode;

[RegisterComponent]
public sealed partial class WeaponRandomExplodeComponent : Component
{
    [DataField]
    public float ExplosionChance;

    /// <summary>
    /// if not filled - the explosion force will be 1.
    /// if filled - the explosion force will be the current charge multiplied by this.
    /// </summary>
    [DataField]
    public float MultiplyByCharge;
}
