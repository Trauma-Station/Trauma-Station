// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Lavaland.Shared.Weapons.Block;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlockChargeUserComponent : Component
{
    [ViewVariables]
    public EntityUid BlockingWeapon;
}
