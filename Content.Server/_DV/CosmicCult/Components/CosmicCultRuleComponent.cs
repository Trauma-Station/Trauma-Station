using Content.Server.RoundEnd;
using Content.Shared._DV.CosmicCult.Components;
using Content.Server._DV.CosmicCult.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._DV.CosmicCult.Components;

/// <summary>
/// Component for the CosmicCultRuleSystem that should store gameplay info.
/// </summary>
[RegisterComponent, Access(typeof(CosmicCultRuleSystem), typeof(CosmicChantrySystem), typeof(CosmicCultSystem), typeof(MonumentSystem))] // This is getting ridiculous
[AutoGenerateComponentPause]
public sealed partial class CosmicCultRuleComponent : Component
{
    /// <summary>
    /// What happens if all of the cultists die.
    /// </summary>
    [DataField]
    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;

    /// <summary>
    /// Sender for shuttle call.
    /// </summary>
    [DataField]
    public LocId RoundEndTextSender = "comms-console-announcement-title-centcom";

    /// <summary>
    /// Text for shuttle call.
    /// </summary>
    [DataField]
    public LocId RoundEndTextShuttleCall = "cosmiccult-elimination-shuttle-call";

    /// <summary>
    /// Text for announcement.
    /// </summary>
    [DataField]
    public LocId RoundEndTextAnnouncement = "cosmiccult-elimination-announcement";

    /// <summary>
    /// Time for emergency shuttle arrival.
    /// </summary>
    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(5);

    [DataField]
    public HashSet<EntityUid> Cultists = [];

    /// <summary>
    /// When true, prevents the wincondition state of Cosmic Cult from being changed.
    /// </summary>
    [DataField]
    public bool WinLocked;

    /// <summary>
    /// When true, Malign Rifts are unable to spawn.
    /// </summary>
    [DataField]
    public bool RiftStop;

    /// <summary>
    /// Set to true to send all the relevant data to the cultists once. Used on roundstart to pass the amount of cultists.
    /// </summary>
    [DataField]
    public bool UpdateAllCultists;

    /// <summary>
    /// Chance that a rift spawn will be replaced with a more dangerous fracture.
    /// </summary>
    [DataField]
    public float FractureChance;

    [DataField]
    public EntityUid ActiveChantry;

    [DataField]
    public WinType WinType = WinType.CrewMinor;

    /// <summary>
    ///     The cult's monument
    /// </summary>
    public Entity<MonumentComponent>? MonumentInGame;

    /// <summary>
    ///     Current tier of the cult
    /// </summary>
    [DataField]
    public int CurrentTier = 0;

    /// <summary>
    ///     Amount of cultists that need to be at least <see cref="CurrentTier"> + 1 level for the current tier to increase.
    /// </summary>
    [DataField]
    public int CultistsForNextTier;

    /// <summary>
    ///     Amount of cultists that are at <see cref="CurrentTier"> + 1 level.
    /// </summary>
    [DataField]
    public int CultistsAtNextLevel;

    /// <summary>
    ///     Amount of present crew
    /// </summary>
    [DataField]
    public int TotalCrew;

    /// <summary>
    ///     Amount of cultists that were initially present
    /// </summary>
    [DataField]
    public int InitialCult;

    /// <summary>
    ///     Amount of active cultists that contribute to progression (doesn't include dead)
    /// </summary>
    [DataField]
    public int TotalCult;

    /// <summary>
    ///     Percentage of crew that have been converted into cultists
    /// </summary>
    [DataField]
    public double PercentConverted;

    /// <summary>
    ///     How much entropy has been siphoned by the cult
    /// </summary>
    [DataField]
    public int EntropySiphoned;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ExtraRiftTimer;

    /// <summary>
    /// Used to prevent recursion with IncreaseTier and UpdateCultData
    /// </summary>
    public bool IncreasingTier;
}

public enum WinType : byte // TODO make a gentle sledgehammer pass over this
{
    /// <summary>
    ///    Cult major win. The Cosmic Cult beckoned the final curtain call.
    /// </summary>
    CultMajor,
    /// <summary>
    ///    Cult minor win. More than half of the cultists are still alive and free.
    /// </summary>
    CultMinor,
    /// <summary>
    ///     Neutral. More than half of the cult are dead and not even on centcomm.
    /// </summary>
    Neutral,
    /// <summary>
    ///     Crew minor win. More than half of the cultists are arrested.
    /// </summary>
    CrewMinor,
    /// <summary>
    ///     Crew major win. All cultists arrested.
    /// </summary>
    CrewMajor,
}
