// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Weapons.SmartGun;

[RegisterComponent, NetworkedComponent]
public sealed partial class SmartGunComponent : Component
{
    [DataField]
    public bool RequiresWield;
}
