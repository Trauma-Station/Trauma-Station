// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Nuclear.Reactor;

/// <summary>
/// Main component for a nuclear reactor.
/// </summary>
/// <remarks>
/// Values inspired by goonstation reactor from https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/nuclearreactor.dm
/// </remarks>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class NuclearReactorComponent : Component
{
    /// <summary>
    /// The name of <see cref="PartsContainer"/>.
    /// </summary>
    [DataField]
    public string PartsContainerName = "reactor_parts";

    /// <summary>
    /// Container that the part entities are stored in.
    /// </summary>
    [ViewVariables]
    public Container PartsContainer = default!;

    /// <summary>
    /// Width of the reactor grid
    /// </summary>
    [DataField]
    public int GridWidth = 7;

    /// <summary>
    /// Height of the reactor grid
    /// </summary>
    [DataField]
    public int GridHeight = 7;

    [DataField]
    public int ReactorOverheatTemp = 1200;
    [DataField]
    public int ReactorFireTemp = 1500;
    [DataField]
    public int ReactorMeltdownTemp = 2000;

    /// <summary>
    /// The reactor grid storing each slot's part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity?[] PartGrid = Array.Empty<NetEntity?>();

    /// <summary>
    /// The flux grid for processing neutrons.
    /// </summary>
    [DataField(serverOnly: true)]
    public ValueList<ReactorNeutron>[] FluxGrid = Array.Empty<ValueList<ReactorNeutron>>();

    public int GridSize => GridWidth * GridHeight;

    public int GridIndex(int x, int y)
        => x + y * GridWidth;

    public NetEntity? GetPart(int x, int y)
        => PartGrid[GridIndex(x, y)];

    public ref ValueList<ReactorNeutron> GetFlux(int x, int y)
        => ref FluxGrid[GridIndex(x, y)];

    public void SetPart(int x, int y, NetEntity? part)
    {
        PartGrid[GridIndex(x, y)] = part;
    }

    /// <summary>
    /// Number of neutrons that hit the edge of the reactor grid last tick
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RadiationLevel;

    /// <summary>
    /// Gas mixture currently in the reactor
    /// </summary>
    [DataField]
    public GasMixture? AirContents;

    /// <summary>
    /// Reactor casing temperature
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// Thermal mass. Basically how much energy it takes to heat this up 1Kelvin
    /// </summary>
    [DataField]
    public float ThermalMass = 420 * 2000; // specific heat capacity of steel (420 J/kgK) * mass of reactor (kg)

    /// <summary>
    /// Volume of gas to process each tick
    /// </summary>
    [DataField]
    public float ReactorVesselGasVolume = 200;

    /// <summary>
    /// Flag indicating the reactor is overheating
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsSmoking;

    /// <summary>
    /// Flag indicating the reactor is on fire
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsBurning;

    /// <summary>
    /// Flag indicating total meltdown has happened
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Melted;

    /// <summary>
    /// The set insertion level of the control rods
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ControlRodInsertion = 2;

    /// <summary>
    /// The averaged, actual insertion level of the control rods
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AvgInsertion;

    /// <summary>
    /// Sound that plays globally on meltdown
    /// </summary>
    [DataField]
    public SoundSpecifier MeltdownSound = new SoundPathSpecifier("/Audio/_FarHorizons/Machines/meltdown_siren.ogg");

    /// <summary>
    /// Radio channel to send alerts to
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> AlertsChannel = "Engineering";

    /// <summary>
    /// Last reported temperature during overheat events
    /// </summary>
    [DataField]
    public float LastSendTemperature = Atmospherics.T20C;

    /// <summary>
    /// If the reactor has given the nuclear emergency warning
    /// </summary>
    [DataField]
    public bool HasSentWarning;

    /// <summary>
    /// Alert level to set after meltdown
    /// </summary>
    [DataField]
    public string MeltdownAlertLevel = "yellow";

    /// <summary>
    /// The minimum radiation from the melted reactor
    /// </summary>
    [DataField]
    public float MeltdownRadiation = 10;

    /// <summary>
    /// How quickly radiation decreases
    /// </summary>
    /// <remarks>Cannot be less than 1</remarks>
    [DataField]
    public float RadiationStability = 2;

    /// <summary>
    /// The soft maximum radiation the reactor is expected to produce, beyond which radiation increases logarithmically. Also used for alarms and UI.
    /// </summary>
    [DataField]
    public float MaximumRadiation = 30;

    /// <summary>
    /// The maximum thermal power the reactor is expected to produce
    /// </summary>
    /// <remarks>This will NOT stop the reactor from making more than this value</remarks>
    [DataField]
    public int MaximumThermalPower = 10000000;

    /// <summary>
    /// The estimated thermal power the reactor is making in Watts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ThermalPower;
    [DataField]
    public int ThermalPowerCount;
    [DataField]
    public int ThermalPowerPrecision = 128;

    [DataField]
    public EntityUid? AlarmAudioHighThermal;
    [DataField]
    public EntityUid? AlarmAudioHighTemp;
    [DataField]
    public EntityUid? AlarmAudioHighRads;

    [DataField]
    public string PartSlotId = "part_slot";

    [ViewVariables]
    public ItemSlot PartSlot = default!;

    /// <summary>
    /// The selected prefab, or null for a random layout.
    /// </summary>
    [DataField]
    public ProtoId<NuclearReactorPrefabPrototype>? Prefab = "7x7Normal";

    /// <summary>
    /// Chance that a reactor slot is filled when applying the random prefab
    /// </summary>
    [DataField]
    public float RandomPrefabFill = 0.3f;

    /// <summary>
    /// Determines the spacing and position of the visual grid. Measured in pixels.
    /// </summary>
    /// <remarks>
    /// [0] Spacing along the x axis<br/>
    /// [1] Spacing along the y axis<br/>
    /// [2] Offset of the center along the x axis<br/>
    /// [3] Offset of the center along the y axis
    /// </remarks>
    [DataField]
    public int[] Gridbounds = [ 18, 15, 0, 5 ];

    #region Device Network
    /// <summary>
    /// The sink port to set control rod insertion.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> ControlRodInsertionPort = "ControlRodInsertion";

    /// <summary>
    /// The source port to send average insertion of control rods.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> ControlRodsAvgPort = "ControlRodsAvg";

    /// <summary>
    /// The source port to send <see cref="Temperature"/> to.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> CasingTempPort = "CasingTemperature";

    /// <summary>
    /// The source port to send <see cref="ThermalPower"/> to.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> ThermalPowerPort = "ReactorThermalPower";

    [DataField]
    public int LastSentTemp;
    #endregion

    [ViewVariables]
    public EntityUid LastUser;

    [AutoPausedField]
    public TimeSpan? NextLog;
}
