// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.Enchanting.Components;

/// <summary>
/// Marker component added to altars to let items be enchanted on them and allow mob sacrificing to upgrade tiers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EnchantingTableComponent : Component
{
    /// <summary>
    /// Optional whitelist the user has to match to use it.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;
}