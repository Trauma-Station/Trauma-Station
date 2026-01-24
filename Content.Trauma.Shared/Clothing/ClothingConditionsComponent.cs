// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Clothing;

/// <summary>
/// Checks entity conditions on the wearer before trying to equip some clothing.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ClothingConditionsSystem))]
public sealed partial class ClothingConditionsComponent : Component
{
    /// <summary>
    /// The conditions to check against the wearer.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition[] Conditions = default!;

    /// <summary>
    /// Reason shown to the user.
    /// Gets passed identity entity "target" and bool "self".
    /// </summary>
    [DataField(required: true)]
    public LocId Reason;
}
