// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Samurai;

[RegisterComponent]
public sealed partial class EnergyPolearmComponent : Component
{
    /// <summary>
    /// The time being hit by this entity will stun you.
    /// </summary>
    [DataField]
    public String Mode;

    /// <summary>
    /// The time being hit by this entity will stun you.
    /// </summary>
    [DataField]
    public TimeSpan ThrownStunTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The time being hit by this entity will stun you.
    /// </summary>
    [DataField]
    public TimeSpan StaffWideSwingStunTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The time after wideswinging it takes before being able to use the polearm again.
    /// </summary>
    [DataField]
    public TimeSpan StaffWideSwingUseDelayPerTarget = TimeSpan.FromSeconds(.5);

    [DataField]
    public string StaffUseDelay = "energyPolearm";
}
