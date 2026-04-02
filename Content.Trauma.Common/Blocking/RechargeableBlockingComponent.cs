// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Blocking;

[RegisterComponent, NetworkedComponent]
public sealed partial class RechargeableBlockingComponent : Component
{
    [DataField]
    public float DischargedRechargeRate = 1.33f;

    [DataField]
    public float ChargedRechargeRate = 2f;

    /// <summary>
    /// Percentage of maxCharge to be able to activate item again.
    /// </summary>
    [DataField]
    public float RechargePercentage = 0.1f;

    [DataField]
    public bool Discharged = true;
}
