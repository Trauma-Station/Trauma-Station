// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.RoundEnd;
using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Server.CosmicCult.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.CosmicCult.Components;

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
    /// When true, Malign Rifts are unable to spawn.
    /// </summary>
    [DataField]
    public bool RiftStop;

    /// <summary>
    /// Set to true to send all the relevant data to the cultists once. Used on roundstart to pass the amount of cultists.
    /// </summary>
    [DataField]
    public bool UpdateAllCultists;

    [DataField]
    public EntityUid ActiveChantry;

    [DataField]
    public WinType WinType = WinType.CrewWin; // If the cult didn't summon the Unknown, that's a crew win

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

public enum WinType : byte
{
    CultWin,
    CrewWin,
}
