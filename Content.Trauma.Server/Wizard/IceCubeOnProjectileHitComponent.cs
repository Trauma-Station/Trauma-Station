// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Server.Wizard;

[RegisterComponent]
public sealed partial class IceCubeOnProjectileHitComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();
}
