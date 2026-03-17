// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Blocking;

/// <summary>
/// Component for shields that use a battery for power.
/// Draining the battery enough changes the recharge rate until it recharges enough.
/// The shield can't be used while discharged.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RechargeableBlockingSystem))]
[AutoGenerateComponentState]
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

    [DataField, AutoNetworkedField]
    public bool Discharged;
}
