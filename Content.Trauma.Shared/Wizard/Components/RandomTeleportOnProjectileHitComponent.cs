// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Wizard.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RandomTeleportOnProjectileHitComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();
}
