using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Shared._Orion.Bitrunning.Prototypes;

[Prototype("bitrunningVirtualDomain")]
public sealed partial class BitrunningVirtualDomainPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Server points required to start this domain.
    /// </summary>
    [DataField]
    public int Cost { get; private set; }

    [DataField]
    public BitrunningDifficulty Difficulty { get; private set; } = BitrunningDifficulty.Easy;

    /// <summary>
    /// Optional loot tier override used when selecting delivery reward loot tables.
    /// </summary>
    [DataField]
    public BitrunningDifficulty? RewardLootDifficulty { get; private set; }

    /// <summary>
    /// Server/domain reward for successful completion.
    /// </summary>
    [DataField]
    public int ServerRewardPoints { get; private set; }

    /// <summary>
    /// Personal bitrunning points reward granted per participant on successful completion.
    /// </summary>
    [DataField]
    public int BitrunningRewardPoints { get; private set; }

    /// <summary>
    /// Additional server points granted for successful completion of a randomized run.
    /// </summary>
    [DataField]
    public int RandomServerBonusPoints { get; private set; }

    /// <summary>
    /// Additional personal bitrunning points granted per participant for successful completion of a randomized run.
    /// </summary>
    [DataField]
    public int RandomBitrunningBonusPoints { get; private set; }

    [DataField(required: true, customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath MapPath { get; private set; }

    /// <summary>
    /// Marks this domain as modular in UI/data for future modular segment pipelines.
    /// Also randomize spawn of encrypted cache and cache crate if grid have multiple marks!
    /// </summary>
    [DataField]
    public bool IsModular { get; private set; }

    /// <summary>
    /// Indicates availability of secondary objectives/loot hooks for this domain.
    /// </summary>
    [DataField]
    public bool HasSecondaryObjectives { get; private set; }

    /// <summary>
    /// If true, domain info is redacted until scanner/points thresholds are met.
    /// </summary>
    [DataField]
    public bool HiddenUntilScanned { get; private set; } = true;

    /// <summary>
    /// Minimum scanner tier required to reveal non-redacted domain identity/details.
    /// </summary>
    [DataField]
    public int RequiredScannerTier { get; private set; } = 1;

    /// <summary>
    /// Extra server points buffer applied when revealing hidden domain names.
    /// </summary>
    [DataField]
    public int NameRevealPointBuffer { get; private set; } = 5;

    /// <summary>
    /// Minimum server points required to reveal reward value in UI.
    /// </summary>
    [DataField]
    public int RequiredPointsToRevealReward { get; private set; }

    /// <summary>
    /// Defines how the domain is completed.
    /// </summary>
    [DataField]
    public BitrunningObjectiveType ObjectiveType { get; private set; } = BitrunningObjectiveType.None;

    /// <summary>
    /// Optional objective pool. If set, one objective is selected randomly on run start.
    /// </summary>
    [DataField]
    public BitrunningObjectiveType[] ObjectiveTypePool { get; private set; } = [];

    /// <summary>
    /// Objective progress needed to trigger reward flow.
    /// </summary>
    [DataField]
    public int ObjectiveTarget { get; private set; } = 1;

    /// <summary>
    /// Optional objective-specific target overrides.
    /// </summary>
    [DataField]
    public Dictionary<BitrunningObjectiveType, int> ObjectiveTargetByType { get; private set; } = [];

    /// <summary>
    /// If true, objective completion spawns a reward cache crate in the domain.
    /// </summary>
    [DataField]
    public bool SpawnRewardCacheOnObjectiveComplete { get; private set; } = true;

    /// <summary>
    /// If true, the run is automatically stopped after objective completion.
    /// </summary>
    [DataField]
    public bool AutoStopOnObjectiveComplete { get; private set; }

    /// <summary>
    /// If true, avatar entities are deleted once a bitrunner disconnects from them.
    /// </summary>
    [DataField]
    public bool DeleteAvatarOnDisconnect { get; private set; }

    /// <summary>
    /// If false, bitrunning disk modifications are disabled in this domain.
    /// </summary>
    [DataField]
    public bool AllowDiskModifications { get; private set; } = true;

    /// <summary>
    /// If false, loading avatar appearance/species from the player selected profile is disabled in this domain.
    /// </summary>
    [DataField]
    public bool AllowProfileLoad { get; private set; } = true;

    /// <summary>
    /// Optional forced loadout for this domain. Has priority over netpod selection.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? ForcedLoadout { get; private set; }

    /// <summary>
    /// Optional forced loadout specie for this domain. Has priority over netpod selection.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, ProtoId<StartingGearPrototype>> ForcedLoadoutSpecie { get; private set; } = new();

    /// <summary>
    /// Fixed domain completion loot to spawn in decrypted cache.
    /// Key: entity prototype, Value: amount.
    /// </summary>
    [DataField]
    public Dictionary<string, int> CompletionLoot { get; private set; } = [];
}
