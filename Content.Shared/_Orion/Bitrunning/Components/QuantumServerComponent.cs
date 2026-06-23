using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Content.Shared.EntityTable;
using Robust.Shared.Audio;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QuantumServerComponent : Component
{
    // This is intentionally unbounded so avatars and linked pods always remain viewable on camera networks.
    private const int UnboundedBroadcastRange = int.MaxValue;

    [DataField, AutoNetworkedField]
    public BitrunningServerState State = BitrunningServerState.Ready;

    [DataField, AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public int ScannerTier = 1;

    [DataField, AutoNetworkedField]
    public float CooldownEfficiency = 1f;

    [DataField, AutoNetworkedField]
    public float QualityBonus;

    [DataField, AutoNetworkedField]
    public EntProtoId AvatarPrototype = "MobHumanRandom";

    /// <summary>
    /// Crate that spawns in domain as reward when players reach goal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId CompletionRewardCachePrototype = "BitrunningObjectiveCacheStructure";

    /// <summary>
    /// Crate that spawns in byteforge delivery.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId RewardCachePrototype = "CrateBitrunSecureReward";

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEndTime;

    [DataField, AutoNetworkedField]
    public bool BroadcastEnabled;

    [DataField]
    public int BroadcastWirelessRange = UnboundedBroadcastRange;

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryPeacefulLootTable = "BitrunningDeliveryPeacefulLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryEasyLootTable = "BitrunningDeliveryEasyLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryMediumLootTable = "BitrunningDeliveryMediumLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryHardLootTable = "BitrunningDeliveryHardLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryExtremeLootTable = "BitrunningDeliveryExtremeLoot";

    [DataField]
    public TimeSpan ExitParalyzeTime = TimeSpan.FromSeconds(3.5);

    [DataField]
    public TimeSpan ExitBlindnessTime = TimeSpan.FromSeconds(3.5);

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<BitrunningVirtualDomainPrototype>)), AutoNetworkedField]
    public string? CurrentDomain;

    // Server-only runtime state. These fields are not synchronized to clients.
    public EntityUid? DomainMapUid;

    public EntityUid? DomainGridUid;

    public readonly HashSet<EntityUid> ActiveConnections = new();

    public EntityCoordinates? ExitCoordinates;

    public EntityCoordinates? CacheCoordinates;

    public bool HasExplicitCacheMarker;

    public EntityCoordinates? GoalCoordinates;

    public EntityCoordinates? SpawnCoordinates;

    public EntityUid? LinkedByteforge;

    public TimeSpan DomainStartTime;

    public int ObjectivePoints;

    public TimeSpan NextSatiationProgressTime;

    public int ObjectiveGoal;

    public bool ObjectiveCompleted;

    public BitrunningObjectiveType ObjectiveType = BitrunningObjectiveType.None;

    public int ThreatsSpawned;

    public bool AllowDiskModifications = true;

    public bool AllowProfileLoad = true;

    public bool WasRandomizedRun;

    public readonly HashSet<EntityUid> GrantedItemDisks = new();

    [DataField]
    public SoundSpecifier DomainStartSound = new SoundPathSpecifier("/Audio/_Orion/Machines/terminal/terminal_processing.ogg");

    [DataField]
    public SoundSpecifier DomainLoadedSound = new SoundPathSpecifier("/Audio/_Orion/Machines/terminal/terminal_insert_disc.ogg");

    [DataField]
    public SoundSpecifier DomainStopSound = new SoundPathSpecifier("/Audio/_Orion/Machines/terminal/terminal_off.ogg");

    [DataField]
    public SoundSpecifier DomainAlertSound = new SoundPathSpecifier("/Audio/_Orion/Machines/terminal/terminal_alert.ogg");

    [DataField]
    public SoundSpecifier ObjectiveRewardSound = new SoundPathSpecifier("/Audio/_Orion/Machines/terminal/terminal_success.ogg");
}
