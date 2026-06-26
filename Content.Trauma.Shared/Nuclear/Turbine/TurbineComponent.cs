// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.DeviceLinking;
using Content.Shared.Tools;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Nuclear.Turbine;

/// <summary>
/// Component for a steam turbine that generates power from hot water vapor.
/// </summary>
/// <remarks>
/// Values inspired by goonstation's https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/turbine.dm
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class TurbineComponent : Component
{
    /// <summary>
    /// Power generated last tick
    /// </summary>
    [DataField, AutoNetworkedField]
    public int LastGen;

    /// <summary>
    /// Power actually used last tick
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PowerSupply;

    /// <summary>
    /// Joules per revolution
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StatorLoad = 35000;

    /// <summary>
    /// Minimum setting of stator load
    /// </summary>
    [DataField]
    public float MinStatorLoad = 1000;

    /// <summary>
    /// Maximum setting of stator load
    /// </summary>
    [DataField]
    public float MaxStatorLoad = 500000;

    /// <summary>
    /// Current RPM of turbine
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RPM = 0;

    /// <summary>
    /// Turbine's resistance to change in RPM
    /// </summary>
    [DataField]
    public float TurbineMass = 1000;

    /// <summary>
    /// Most efficient power generation at this value, overspeed at 1.2*this
    /// </summary>
    [DataField]
    public float BestRPM = 600;

    /// <summary>
    /// RPM the animation is playing at clientside
    /// </summary>
    [DataField]
    public float AnimRPM = 0;

    /// <summary>
    /// Volume of gas to process per tick for power generation
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FlowRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// Maximum volume of gas to process per tick
    /// </summary>
    [DataField]
    public float FlowRateMax = Atmospherics.MaxTransferRate * 5;

    [DataField]
    public float OutputPressure = Atmospherics.MaxOutputPressure * 3;

    /// <summary>
    /// Max/min temperatures
    /// </summary>
    [DataField]
    public float MaxTemp = 3000;
    [DataField]
    public float MinTemp = Atmospherics.T20C;

    /// <summary>
    /// Health of the turbine
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BladeHealth = 15;

    /// <summary>
    /// Maximum health of the turbine
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BladeHealthMax = 15;

    /// <summary>
    /// If the turbine is functional or not
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Ruined;

    /// <summary>
    /// Flag indicating the turbine is sparking
    /// </summary>
    [DataField]
    public bool IsSparking;

    /// <summary>
    /// Flag indicating the turbine is smoking
    /// </summary>
    [DataField]
    public bool IsSmoking;

    /// <summary>
    /// Flag for indicating that energy available is less than needed to turn the turbine
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Stalling;

    /// <summary>
    /// Flag for RPM being > BestRPM*1.2
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Overspeed;

    /// <summary>
    /// Flag for gas temperature being > MaxTemp - 500
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Overtemp;

    /// <summary>
    /// Flag for gas temperature being < MinTemp
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Undertemp;

    /// <summary>
    /// Adjustment for power generation
    /// </summary>
    [DataField]
    public float PowerMultiplier = 1;

    [DataField]
    public EntityUid? AlarmAudioOvertemp;
    [DataField]
    public EntityUid? AlarmAudioUnderspeed;

    [DataField]
    public SoundSpecifier? DamageSound = new SoundCollectionSpecifier("TurbineDamage")
    {
        Params = AudioParams.Default.WithVariation(0.25f).WithVolume(-1)
    };

    /// <summary>
    /// Length of repair do-after, in seconds
    /// </summary>
    [DataField]
    public float RepairDelay = 5;

    /// <summary>
    /// Amount of fuel consumed for repair
    /// </summary>
    [DataField]
    public float RepairFuelCost = 15;

    /// <summary>
    /// Tool capability needed to repair
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> RepairTool = "Welding";

    /// <summary>
    /// The blade currently installed in the turbine
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentBlade;

    /// <summary>
    /// The stator currently installed in the turbine
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentStator;

    #region Device Network
    /// <summary>
    /// Circuit source port that gets the current <see cref="RPM"/>.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> SpeedPort = "TurbineSpeed";

    /// <summary>
    /// The proto ID of the "Speed: High" source port
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> SpeedHighPort = "TurbineSpeedHigh";

    /// <summary>
    /// The proto ID of the "Speed: Low" source port
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> SpeedLowPort = "TurbineSpeedLow";

    /// <summary>
    /// Circuit source port that gets the current <see cref="LastGen"/>.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> PowerGenPort = "TurbineGenerated";

    /// <summary>
    /// Circuit source port that gets the current <see cref="LastSupply"/>.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> PowerSupplyPort = "TurbineSupply";

    /// <summary>
    /// Circuit port to set the current stator load.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> StatorLoadPort = "TurbineStatorLoad";

    /// <summary>
    /// Circuit port to set the current flow rate.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> FlowRatePort = "TurbineFlowRate";

    [DataField]
    public int LastSentSpeed = -1;
    [DataField]
    public bool LastSentHigh;
    [DataField]
    public bool LastSentLow;
    #endregion

    #region Debug
    [ViewVariables]
    public bool HasPipes = false;
    [ViewVariables]
    public float LastVolumeTransfer = 0;
    #endregion
}
