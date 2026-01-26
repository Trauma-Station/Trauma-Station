// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Emag;

/// <summary>
/// Only allows this emag to work on entities matching a whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EmagWhitelistSystem))]
[AutoGenerateComponentState]
public sealed partial class EmagWhitelistComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Whitelist = default!;
}
