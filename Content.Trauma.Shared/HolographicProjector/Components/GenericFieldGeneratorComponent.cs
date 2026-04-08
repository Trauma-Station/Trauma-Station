using Content.Shared.DeviceLinking;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Trauma.Shared.HolographicProjector.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class GenericFieldGeneratorComponent : Component
{
    /// <summary>
    /// How much power should this field generator consume every 1/5th of a second?
    /// </summary>
    [DataField]
    public float PowerDrain = 400f;

    /// <summary>
    /// How many tiles should this field check before giving up?
    /// </summary>
    [DataField]
    public float MaxLength = 8F;

    /// <summary>
    /// Is the generator toggled on?
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Is the generator Charged?
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Charged;

    /// <summary>
    /// Is this generator connected to fields?
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool IsConnected;

    /// <summary>
    /// The masks the raycast should not go through
    /// </summary>
    [DataField]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    /// <summary>
    /// The generator that this generator is paired with
    /// </summary>
    public Entity<GenericFieldGeneratorComponent>? ConnectedGenerator; // No VV cuz it looks ass

    /// <summary>
    /// A list of fields created by this generator.
    /// Stores a list of fields connected between generators in this direction.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> ConnectedFields = [];

    /// <summary>
    /// What fields should this spawn?
    /// </summary>
    [DataField("createdField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [AutoNetworkedField]
    public string CreatedField = "ContainmentField";

    /// <summary>
    /// Used to check if it's received power recently.
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan PowerTimer;

    [DataField]
    public TimeSpan PowerTime = TimeSpan.FromSeconds(0.2);

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

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

    [DataField]
    public SoundSpecifier DeactivationSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/field_off.ogg");
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
