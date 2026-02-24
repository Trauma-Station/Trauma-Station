using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's entropic colossus.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class CosmicColossusComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AttackHoldTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AttackAnimationTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DeathTimer = default!;

    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DissolveTimer = default!;

    [DataField] public SoundSpecifier ReawakenSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/colossus_spawn.ogg");

    [DataField] public SoundSpecifier DeathSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/colossus_death.ogg");

    [DataField] public SoundSpecifier IngressSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_ingress.ogg");

    [DataField] public SoundSpecifier DoAfterSfx = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    [DataField] public SoundSpecifier SunderSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_sunder.ogg");

    [DataField] public SoundSpecifier DissolveSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_lapse.ogg");

    [DataField] public EntProtoId CultVfx = "CosmicGenericVFX";

    [DataField] public EntProtoId CultBigVfx = "CosmicGlareAbilityVFX";

    [DataField] public EntProtoId Attack1Vfx = "CosmicColossusAttack1Vfx";

    [DataField] public EntProtoId SunderVfx = "CosmicSunderAbilityVFX";

    [DataField] public EntProtoId DissolveVfx = "CosmicLapseAbilityVFX";

    [DataField] public EntProtoId EffigyPrototype = "CosmicEffigy";

    [DataField] public EntProtoId EffigyObjective = "ColossusEffigyObjective";

    [DataField] public EntProtoId EffigyPlaceAction = "ActionCosmicColossusEffigy";

    [DataField] public EntProtoId Mindsink = "CosmicCultMindSink";

    [DataField] public EntityUid? EffigyPlaceActionEntity;

    [DataField] public float SunderRange = 2.5f;

    [DataField] public float SunderThrowDistance = 3f;

    [DataField] public TimeSpan SunderStun = TimeSpan.FromSeconds(1);

    [DataField] public TimeSpan IngressDoAfter = TimeSpan.FromSeconds(4);

    [DataField] public TimeSpan AttackWait = TimeSpan.FromSeconds(1.5);

    [DataField] public TimeSpan AttackAnimation = TimeSpan.FromSeconds(0.45);

    [DataField] public TimeSpan HibernationWait = TimeSpan.FromSeconds(20);

    [DataField] public TimeSpan DeathWait = TimeSpan.FromMinutes(15);

    [DataField] public TimeSpan DissolveWait = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField] public bool Attacking;

    [DataField, AutoNetworkedField] public bool AttackCharge;

    [DataField, AutoNetworkedField] public bool Hibernating;

    [DataField] public bool Timed;

    /// <summary>
    /// If the colossus was creted by a chantry, the borg goes into this container.
    /// Midround colossi are so ancient the original borg has long decayed into dust or something.
    /// Does that make the colossus a gundam?
    /// </summary>
    [ViewVariables]
    public ContainerSlot Container = default!;

    [ViewVariables]
    public string ContainerId = "container";

    [ViewVariables]
    public EntityUid? ImprisonedEntity => Container?.ContainedEntity;

    /// <summary>
    ///     Entities not affected by entropic sunder
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist SunderBlacklist = new();
}

[Serializable, NetSerializable]
public enum ColossusVisuals : byte
{
    Status,
    Hibernation,
    Sunder,
}

[Serializable, NetSerializable]
public enum ColossusStatus : byte
{
    Alive,
    Dead,
    Action,
}

[Serializable, NetSerializable]
public enum ColossusAction : byte
{
    Running,
    Stopped,
}
