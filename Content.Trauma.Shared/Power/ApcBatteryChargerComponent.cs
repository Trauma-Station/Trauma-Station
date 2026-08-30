// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Power;

/// <summary>
/// Charges this entity's battery from APC power.
/// The battery can be predicted since this updated charge rates instead of updating the charge value every tick.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ApcBatteryChargerSystem))]
[AutoGenerateComponentState]
public sealed partial class ApcBatteryChargerComponent : Component
{
    /// <summary>
    /// The target rate to draw power from the APC powernet at.
    /// Only draws while the battery isn't full.
    /// </summary>
    [DataField(required: true)]
    public float DrawRate;

    /// <summary>
    /// Constant power usage while not charging, added to <see cref="DrawRate"/> while charging.
    /// </summary>
    [DataField]
    public float IdleLoad;

    /// <summary>
    /// The current battery charge rate, updated whenever received power changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ChargeRate;

    /// <summary>
    /// How much energy gets stored in the battery per unit drawn from the APC powernet.
    /// </summary>
    [DataField]
    public float Efficiency = 1f;
}
