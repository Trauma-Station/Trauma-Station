// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Emag;

/// <summary>
/// Gives X amount of limited charges to entity when emagged
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GiveChargeOnEmagComponent : Component
{
    /// <summary>
    /// Number of limited charges to give
    /// </summary>
    [DataField]
    public int Charges = 2;

    /// <summary>
    /// Will this increase MaxCharge if we are recharging over the max limit?
    /// </summary>
    [DataField]
    public bool RaiseMaxCharge = true;
}
