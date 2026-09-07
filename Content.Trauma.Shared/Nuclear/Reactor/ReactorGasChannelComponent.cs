// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;

namespace Content.Trauma.Shared.Nuclear.Reactor;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReactorGasChannelComponent : Component
{
    /// <summary>
    /// How much gas this part can hold, and will be processed per tick.
    /// </summary>
    [DataField(required: true)]
    public float GasVolume;

    /// <summary>
    /// How adept the gas channel is at transfering heat to/from gasses.
    /// </summary>
    [DataField]
    public float GasThermalCrossSection = 15;

    /// <summary>
    /// The gas mixture inside the gas channel.
    /// </summary>
    [DataField]
    public GasMixture? AirContents;
}
