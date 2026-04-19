// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public List<ProtoId<HereticKnowledgePrototype>> BaseKnowledge = new()
    {
        "BreakOfDawn",
        "HeartbeatOfMansus",
        "AmberFocus",
        "LivingHeart",
        "CodexCicatrix",
        "CloakOfShadow",
        "FeastOfOwls",
        "PhylacteryOfDamnation",
    };

    [ViewVariables]
    public Container RitualContainer = default!;

    [DataField, AutoNetworkedField]
    public EntityUid? ChosenRitual;

    /// <summary>
    ///     Contains the list of targets that are eligible for sacrifice.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<SacrificeTargetData> SacrificeTargets = new();

    /// <summary>
    ///     How much targets can a heretic have?
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxTargets = 6;

    /// <summary>
    ///     Indicates a path the heretic is on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HereticPath? CurrentPath;

    /// <summary>
    ///     Indicates a stage of a path the heretic is on. 0 is no path, 10 is ascension
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PathStage;

    [DataField, AutoNetworkedField]
    public bool Ascended;

    [DataField, AutoNetworkedField]
    public bool CanAscend = true;

    [DataField]
    public SoundSpecifier? InfluenceGainSound = new SoundCollectionSpecifier("bloodCrawl");

    [DataField]
    public EntProtoId MansusGraspProto = "TouchSpellMansus";

    [DataField]
    public LocId InfluenceGainBaseMessage = "influence-base-message";

    [DataField]
    public int InfluenceGainTextFontSize = 22;

    [DataField]
    public List<LocId> InfluenceGainMessages = new()
    {
        "influence-gain-message-1",
        "influence-gain-message-2",
        "influence-gain-message-3",
        "influence-gain-message-4",
        "influence-gain-message-5",
        "influence-gain-message-6",
        "influence-gain-message-7",
        "influence-gain-message-7",
        "influence-gain-message-8",
        "influence-gain-message-9",
        "influence-gain-message-10",
        "influence-gain-message-11",
        "influence-gain-message-12",
        "influence-gain-message-13",
        "influence-gain-message-14",
        "influence-gain-message-15",
        "influence-gain-message-16",
    };

    [DataField]
    public List<EntProtoId<ObjectiveComponent>> AllObjectives = new()
    {
        "HereticKnowledgeObjective",
        "HereticSacrificeObjective",
        "HereticSacrificeHeadObjective",
    };

    [DataField, AutoNetworkedField]
    public bool ObjectivesCompleted;

    /// <summary>
    /// Events raised when on new body when mind gets transferred to it
    /// </summary>
    [DataField, NonSerialized]
    public List<HereticKnowledgeEvent> KnowledgeEvents = new();

    /// <summary>
    /// Minions summoned by this heretic
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Minions = new();

    /// <summary>
    /// How much drafts of <see cref="SideDraftChoiceAmount"/> side knowledge heretic currently has.
    /// Side category -> draft amount
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StoreCategoryPrototype>, int> SideKnowledgeDrafts = new()
    {
        { "HereticPathSideT1", 1 }, // 1 free draft of t1 side roundstart
        { "HereticPathSideT2", 0 },
        { "HereticPathSideT3", 0 },
    };

    [DataField]
    public int SideDraftChoiceAmount = 3;

    /// <summary>
    /// After this amount of knowledge heretic loses their blade break ability
    /// </summary>
    [DataField]
    public float LockBladeBreakKnowledgeAmount = 8f;

    [DataField, AutoNetworkedField]
    public float KnowledgeTracker;

    [ViewVariables]
    public bool CanBreakBlade => !Ascended && KnowledgeTracker < LockBladeBreakKnowledgeAmount;

    [ViewVariables]
    public bool ShouldShowAura => CurrentPath != HereticPath.Lock && (Ascended || CanAscend && !CanBreakBlade);

    [DataField]
    public LocId BreakBladeAbilityLostMessage = "heretic-blade-break-ability-lost-message";

    [DataField]
    public LocId AuraVisibleMessage = "heretic-aura-message";

    [DataField]
    public TimeSpan AuraDelayTime = TimeSpan.FromMinutes(1);

    [DataField]
    public EntProtoId HideAuraStatusEffect = "HideHereticAuraStatusEffect";

    [DataField]
    public int SacrificeTracker;

    /// <summary>
    /// Influences gradually spawn with increasing tier after sacrifices
    /// <see cref="SacrificeTracker"/> tracks the amount
    /// </summary>
    [DataField]
    public Dictionary<int, EntProtoId> InfluenceSpawnPerSacrificeAmount = new()
    {
        {1, "EldritchInfluenceT2"},
        {2, "EldritchInfluenceT3"},
    };

    /// <summary>
    /// Inactive means either dead or in jaunt
    /// </summary>
    [DataField]
    public bool IsActive = true;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SacrificeTargetData
{
    [DataField]
    public NetEntity Entity;

    [DataField]
    public HumanoidCharacterProfile Profile;

    [DataField]
    public ProtoId<JobPrototype> Job;
}

[Serializable, NetSerializable]
public enum HereticPath : byte
{
    Ash,
    Void,
    Flesh,
    Rust,
    Blade,
    Lock,
    Cosmos,
}
