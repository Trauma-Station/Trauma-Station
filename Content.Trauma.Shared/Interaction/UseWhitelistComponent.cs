// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Interaction;

/// <summary>
/// Prevents UseInHandEvent if you don't match a whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(UseWhitelistSystem))]
public sealed partial class UseWhitelistComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;
}
