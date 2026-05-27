// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Xenomorphs.Caste;
using Robust.Shared.Audio;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class XenomorphsRuleComponent : Component
{
    [DataField]
    public List<EntityUid> Xenomorphs = new();

    /// <summary>
    /// Total number of each caste this rule has ever had.
    /// Does not get decreased if they die or whatever.
    /// <summary>
    [DataField]
    public Dictionary<ProtoId<XenomorphCastePrototype>, int> TotalCastes = new();

    #region Check

    [DataField]
    public TimeSpan CheckDelay = TimeSpan.FromSeconds(30);

    [ViewVariables]
    public TimeSpan NextCheck;

    #endregion

    #region Announcement

    [DataField]
    public string? Announcement = "xenomorphs-announcement";

    [DataField]
    public SoundSpecifier XenomorphInfestationSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Music/Black_Swarm_Short.ogg")
        {
            Params = AudioParams.Default
                .WithVolume(-16f)
        };

    [DataField]
    public SoundSpecifier XenomorphTakeoverSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Music/Colonial_Marines_The_Final_Battle.ogg")
        {
            Params = AudioParams.Default
                .WithVolume(-14f)
        };

    [DataField]
    public Color AnnouncementColor = Color.Red;

    [DataField]
    public string? NoMoreThreatAnnouncement = "xenomorphs-no-more-threat-announcement";

    [DataField]
    public Color NoMoreThreatAnnouncementColor = Color.Gold;

    [DataField]
    public string? Sender;

    [DataField]
    public TimeSpan MinTimeToAnnouncement = TimeSpan.FromSeconds(400);

    [DataField]
    public TimeSpan MaxTimeToAnnouncement = TimeSpan.FromSeconds(450);

    [ViewVariables]
    public bool Announced;

    [ViewVariables]
    public TimeSpan? AnnouncementTime;

    #endregion

    #region RoundEnd

    [DataField]
    public float XenomorphsShuttleCallPercentage = 0.3f;

    [DataField]
    public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(5);

    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    [DataField]
    public string RoundEndTextShuttleCall = "xenomorphs-win-announcement-shuttle-call";

    [DataField]
    public string RoundEndTextAnnouncement = "xenomorphs-win-announcement";

    [DataField]
    public WinType WinType = WinType.Neutral;

    [DataField]
    public List<WinCondition> WinConditions = new ();

    #endregion
}

public enum WinType : byte
{
    XenoMajor,
    XenoMinor,
    Neutral,
    CrewMinor,
    CrewMajor
}

public enum WinCondition : byte
{
    NukeExplodedOnStation,
    NukeActiveInStation,
    XenoTakeoverStation,
    XenoInfiltratedOnCentCom,
    AllReproduceXenoDead,
    AllCrewDead
}
