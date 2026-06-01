// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Enchanting.Components;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Enchanting;

/// <summary>
/// Interact with an <c>Enchanter</c> to add an enchant to it and delete this item.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EnchantAdderSystem))]
public sealed partial class EnchantAdderComponent : Component
{
    /// <summary>
    /// The enchant to add.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<EnchantComponent> Enchant;

    /// <summary>
    /// How long the doafter lasts.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Sets the target entity's name to this.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Sets the target entity's description to this.
    /// </summary>
    [DataField(required: true)]
    public string Desc = string.Empty;

    /// <summary>
    /// Whitelist for enchanters it can add to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for enchanters it can never add to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
