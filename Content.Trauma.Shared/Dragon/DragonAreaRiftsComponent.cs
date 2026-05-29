// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Dragon;

/// <summary>
/// Component for dragons that restricts rift spawning to areas matching a whitelist.
/// Cannot spawn rifts if you aren't in an area at all either.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DragonAreaRiftsSystem))]
public sealed partial class DragonAreaRiftsComponent : Component
{
    /// <summary>
    /// Whitelist to check the dragon's area against.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist AreaWhitelist = default!;
}
