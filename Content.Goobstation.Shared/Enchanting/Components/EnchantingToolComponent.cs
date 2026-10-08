// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.Enchanting.Components;

/// <summary>
/// Component added to bibles that lets them interact with <see cref="EnchanterComponent"/>
/// on an altar to enchant an item.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EnchantingToolComponent : Component
{
    /// <summary>
    /// Optional whitelist the user has to match to use it.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;
}
