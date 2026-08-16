// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;

namespace Content.Trauma.Server.SpaceArtillery.Components;

[RegisterComponent]
public sealed partial class SpaceArtilleryComponent : Component
{
    /// <summary>
    /// Passive power consumption drawn continuously from the powernet while the gun is operational.
    /// This represents baseline energy upkeep and is not tied to active firing.
    /// </summary>
    [DataField]
    public int PowerUsePassive = 600;

    /// <summary>
    /// Maximum rate at which the battery can recharge when connected to a powernet.
    /// Functions as a throttle for battery regeneration, consistent with BatterySelfRechargerComponent behavior.
    /// </summary>
    [DataField]
    public int PowerChargeRate = 3000;

    /// <summary>
    /// Additional power consumed per shot beyond the configured fire cost.
    /// This value is drained from the internal battery (or from the powernet if battery is insufficient).
    /// Used to simulate power-intensive firing beyond base projectile energy requirements.
    /// </summary>
    [DataField]
    public int PowerUseActive = 6000;

    ///Sink Ports
    /// <summary>
    /// Signal port that makes space artillery fire.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> SpaceArtilleryFirePort = "SpaceArtilleryFire";
}
