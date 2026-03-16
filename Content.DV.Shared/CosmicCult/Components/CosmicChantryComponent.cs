using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's Vacuous Chantry.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicChantryComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan SpawnTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CountdownTimer = default!;

    [DataField] public TimeSpan SpawningTime = TimeSpan.FromSeconds(2.4);

    [DataField] public TimeSpan EventTime = TimeSpan.FromSeconds(150);

    [DataField] public bool Spawned;

    [DataField] public bool Completed;

    [DataField] public SoundSpecifier ChantryAlarm = new SoundPathSpecifier("/Audio/_DV/CosmicCult/chantry_alarm.ogg");

    [DataField] public SoundSpecifier BriefingSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/antag_cosmic_AI_briefing.ogg");

    [DataField] public SoundSpecifier ChantryDestructionAnnouncement = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");

    [DataField] public EntProtoId Colossus = "MobCosmicColossus";

    [DataField] public EntProtoId SpawnVFX = "CosmicGlareAbilityVFX";

    [DataField] public EntProtoId Mindsink = "CosmicCultMindSink";

    [DataField] public ContainerSlot Container = default!;

    [DataField] public string ContainerId = "container";

    [ViewVariables] public EntityUid? Victim => Container?.ContainedEntity;
}

[Serializable, NetSerializable]
public enum ChantryVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum ChantryStatus : byte
{
    Off,
    On,
}
