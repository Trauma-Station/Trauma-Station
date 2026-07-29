// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;

namespace Content.Trauma.Shared.Nuclear.Reactor;

/// <summary>
/// A reactor part for the reactor grid.
/// </summary>
/// <remarks>
/// Values inspired by https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/reactorcomponents.dm
/// </remarks>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class ReactorPartComponent : Component
{
    /// <summary>
    /// Position in a reactor's grid.
    /// Null if not yet installed in a reactor.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2i? Position;

    /// <summary>
    /// Icon of this part as it shows in the UIs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string IconStateInserted = "base";

    /// <summary>
    /// Icon of this part as it shows in the world.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string IconStateCap = "rod_cap";

    #region Variables
    /// <summary>
    /// Temperature of this part in Kelvin, defaults to room tmep.
    /// </summary>
    [DataField]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// How much does this part share heat with surrounding parts? Basically surface area in contact (m2).
    /// </summary>
    [DataField]
    public float ThermalCrossSection = 10;

    /// <summary>
    /// How adept is this part at interacting with neutrons - fuel rods are set up to capture them, heat exchangers are set up not to.
    /// </summary>
    [DataField]
    public float NeutronCrossSection = 0.5f;

    /// <summary>
    /// Chance to reflect a neutron instead of absorbing it / letting it pass.
    /// </summary>
    [DataField]
    public float ReflectChance;

    /// <summary>
    /// Max health to set <see cref="MeltHealth"/> to on init.
    /// </summary>
    [DataField]
    public float MaxHealth = 100;

    /// <summary>
    /// Essentially indicates how long this part can be at a dangerous temperature before it melts.
    /// </summary>
    [DataField]
    public float MeltHealth = 100;

    /// <summary>
    /// If this part is melted, you can't take it out of the reactor and it might do some weird stuff.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Melted;

    /// <summary>
    /// The dangerous temperature above which this part starts to melt. 1700K is the melting point of steel.
    /// </summary>
    [DataField]
    public float MeltingPoint = 1700;

    /// <summary>
    /// Thermal mass. Basically how much energy it takes to heat this up by 1 Kelvin.
    /// </summary>
    [DataField(required: true)]
    public float ThermalMass;
    #endregion
}
