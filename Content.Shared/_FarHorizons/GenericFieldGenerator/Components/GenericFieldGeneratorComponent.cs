using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.DeviceLinking;

namespace Content.Shared._FarHorizons.GenericFieldGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GenericFieldGeneratorComponent : Component
{
    /// <summary>
    /// How much power should this field generator consume every 1/5th of a second?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("powerDrain")]
    public float PowerDrain = 10f;

    /// <summary>
    /// How many tiles should this field check before giving up?
    /// </summary>
    [DataField("maxLength")]
    public float MaxLength = 8F;

    /// <summary>
    /// Is the generator toggled on?
    /// </summary>
    [DataField("enabled")]
    public bool Enabled;

    /// <summary>
    /// Is the generator Charged?
    /// </summary>
    [DataField]
    public bool Charged;

    /// <summary>
    /// Is this generator connected to fields?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsConnected;

    /// <summary>
    /// The masks the raycast should not go through
    /// </summary>
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    /// <summary>
    /// A collection of connections that the generator has based on direction.
    /// Stores a list of fields connected between generators in this direction.
    /// </summary>
    [ViewVariables]
    public (Entity<GenericFieldGeneratorComponent>, List<EntityUid>)? Connections;

    /// <summary>
    /// What fields should this spawn?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreatedField = "ContainmentField";

    /// <summary>
    /// How fast should the generator charge?
    /// </summary>
    [DataField]
    public int ChargeRate = 100;

    /// <summary>
    /// Used to check if it's received power recently.
    /// </summary>
    [DataField("accumulator")]
    public float Accumulator;

    /// <summary>
    /// Used to retry connection when fully charged, but not connected
    /// </summary>
    [DataField("retryWait")]
    public float RetryWait;
    
    /// <summary>
    /// Used to retry connection when fully charged, but not connected
    /// </summary>
    public bool Removing = false;

    //Ports
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    [DataField]
    public ProtoId<SourcePortPrototype> ConnectionStatusPort = "ConnectionStatus";
    
    [DataField] 
    public ProtoId<SourcePortPrototype> FieldConnectedPort = "FieldConnected";

    [DataField] 
    public ProtoId<SourcePortPrototype> FieldDisconnectedPort = "FieldDisconnected";
}

[Serializable, NetSerializable]
public enum GenericFieldGeneratorVisuals : byte
{
    PowerLight,
    ConnectionLight,
    OnLight,
    ChargeLight,
}

[Serializable, NetSerializable]
public enum PowerLevelVisuals : byte
{
    NoPower,
    MinimalPower,
    LowPower,
    MediumPower,
    HighPower,
    VeryHighPower,
    FullPower,
}
