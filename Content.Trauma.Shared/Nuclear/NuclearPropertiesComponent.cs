// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear;

/// <summary>
/// An object with material properties important to a nuclear reactor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NuclearPropertiesComponent : Component
{
    private const double DefaultCoefficient = 5.0;

    /// <summary>
    /// Color used for reactor part sprites.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    [DataField("electrical")]
    public float ElectricalConductivity = 5;

    [DataField("thermal")]
    public float ThermalConductivity = 5;

    [DataField("hard")]
    public float Hardness = 3;

    [DataField]
    public float Density = 3;

    [DataField("reflective")]
    public float Reflectivity;

    [DataField("flammable")]
    public float Flammability = 1;

    [DataField("chemical")]
    public float ChemicalResistance = 3;

    [DataField("radioactive")]
    public float Radioactivity;

    [DataField("n_radioactive")]
    public float NeutronRadioactivity;

    [DataField("molitz_bubbles")]
    public float GasPockets;

    [DataField("plasma_offgas")]
    public float ActivePlasma;

    /// <summary>
    /// How much spent fuel is available.
    /// </summary>
    [DataField]
    public float SpentFuel;

    /// <summary>
    /// Calculate the heat transfer coefficient between 2 materials.
    /// </summary>
    /// <remarks>
    /// Either in W/(m^2 K) or made up nonsense units, who knows!
    /// </remarks>
    public static double CalculateHeatTransferCoefficient(NuclearPropertiesComponent? materialA, NuclearPropertiesComponent? materialB)
    {
        var hTC1 = materialA?.BaseTransferCoefficient() ?? DefaultCoefficient;
        var hTC2 = materialB?.BaseTransferCoefficient() ?? DefaultCoefficient;
        return ((Math.Pow(10, hTC1 / 5) - 1) + (Math.Pow(10, hTC2 / 5) - 1)) / 2;
    }

    public double CalculateHeatTransferCoefficient()
        => CalculateHeatTransferCoefficient(this, null);

    // TODO: change heat transfer to actually be physically based this is a fucking meme
    private double BaseTransferCoefficient()
    {
        if (ThermalConductivity > 0 && ElectricalConductivity > 0)
            return (ThermalConductivity + ElectricalConductivity) / 2;
        if (ThermalConductivity > 0)
            return ThermalConductivity;
        if (ElectricalConductivity > 0)
            return ElectricalConductivity;
        return DefaultCoefficient;
    }
}

/// <summary>
/// Sprite layer that gets its Color set to the NuclearProperties's Color field.
/// </summary>
public enum NuclearPropertiesVisuals : byte
{
    Layer
}
