// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Wizard.Traps;

[RegisterComponent, NetworkedComponent]
public sealed partial class IceCubeOnProjectileHitComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();
}
