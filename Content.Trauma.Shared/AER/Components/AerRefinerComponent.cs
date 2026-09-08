using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// This is an entity storage that activates aer cores used for spawning aers
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AerRefinerSystem))]
public sealed partial class AerRefinerComponent : Component
{
    /// <summary>
    /// if the refiner is actively refining
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Refining;

    /// <summary>
    /// When the current refining will end.
    /// </summary>
    [DataField]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan RefineEndTime;

    [DataField]
    public required GroupSelector SpawnTable;

    [DataField, AutoNetworkedField]
    public List<(EntProtoId spawn, double weight)> AvailableSpawn = [];

    /// <summary>
    /// The total duration of the refining.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RefineDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A whitelist specifying what items can be refined
    /// </summary>
    [DataField]
    public EntityWhitelist RefiningWhitelist = new();

    /// <summary>
    /// The ID for <see cref="OutputContainer"/>
    /// </summary>
    [DataField]
    public string OutputContainerName = "output_container";

    /// <summary>
    /// Sound played at the end of a successful Refining.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? RefiningCompleteSound = new SoundCollectionSpecifier("MetalCrunch");

    /// <summary>
    /// Sound played throughout the entire Refining. Cut off if ended early.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? RefiningSound = new SoundPathSpecifier("/Audio/Effects/hydraulic_press.ogg");

    /// <summary>
    /// Stores entity of <see cref="RefiningSound"/> to allow ending it early.
    /// </summary>
    [DataField]
    public EntityUid? RefiningSoundEntity;
}

[Serializable, NetSerializable]
public enum AerRefinerVisuals : byte
{
    Refining
}