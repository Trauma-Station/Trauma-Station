// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Actions.Vampires;

/// <summary>
/// Component that requires an action to check on the performer
/// if they have enough <see cref="VampireComponent.UsableBlood"/> before performing it.
///
/// Consumes the usable blood on succession
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VampireCostActionComponent : Component
{
    /// <summary>
    /// The <see cref="VampireComponent.UsableBlood"/> required to run this action.
    /// </summary>
    [DataField(required: true)]
    public int BloodCost;

    [DataField]
    public string Popup = "You do not have enough usable blood to run this action";
}
