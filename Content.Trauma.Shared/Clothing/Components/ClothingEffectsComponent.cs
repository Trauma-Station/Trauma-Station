// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Clothing.Components;

/// <summary>
/// Component that runs effects when you equip, and optionally, un-equip a clothing item.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingEffectsComponent : Component
{
    /// <summary>
    /// Effects to run when the item is equipped
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] EquipEffects = default!;

    /// <summary>
    /// Effects to run when the item is un-equipped
    /// </summary>
    [DataField]
    public EntityEffect[]? UnEquipEffects;
}
