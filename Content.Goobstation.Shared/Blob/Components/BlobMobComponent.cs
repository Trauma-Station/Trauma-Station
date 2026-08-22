// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlobMobComponent : Component
{
    [DataField]
    public DamageSpecifier HealthOfPulse = new()
    {
        DamageDict = new()
        {
            { "Blunt", -4 },
            { "Slash", -4 },
            { "Piercing", -4 },
            { "Ballistic", -4 },
            { "Heat", -4 },
            { "Cold", -4 },
            { "Shock", -4 },
            { "Poison", -4 },
            { "Radiation", -4 },
            { "Cellular", -4 }
        }
    };
}
