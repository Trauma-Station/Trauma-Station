using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Trauma.Shared.Spy;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class SpyRuleComponent : Component
{
    [DataField]
    public bool GiveUplink = true;

    [DataField]
    public bool GiveBriefing = true;

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Trauma/Ambience/Antag/spy.ogg");

    [DataField]
    public int NumBounties = 10;

    [DataField]
    public ProtoId<WeightedRandomPrototype> BountyPoolProto = "SpyBountyPool";

    [DataField]
    public HashSet<ProtoId<SpyBountyPrototype>> UnavailableBounties = new();

    [DataField]
    public HashSet<ProtoId<SpyBountyPrototype>> ClaimedBounties = new();

    [DataField]
    public Dictionary<ProtoId<SpyBountyPrototype>, float>? BountyPool;

    [DataField]
    public HashSet<SpyBounty> CurrentBounties = new();

    [DataField]
    public TimeSpan RefreshTime = TimeSpan.FromMinutes(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextRefresh = TimeSpan.Zero;

    // Difficulty -> ([ListingPrototype.ID OR SpyRewardPrototype.ID] -> weight)
    [DataField]
    public Dictionary<SpyBountyDifficulty, Dictionary<string, float>> LootPool = new();

    [DataField]
    public SortedDictionary<FixedPoint2, SpyBountyDifficulty> CostToDifficulty = new()
    {
        {0, SpyBountyDifficulty.Easy},
        {30, SpyBountyDifficulty.Medium},
        {60, SpyBountyDifficulty.Hard},
    };

    [DataField]
    public Dictionary<SpyBountyDifficulty, float> ChancesToRemoveRewardFromPool = new()
    {
        {SpyBountyDifficulty.Easy, 0.25f},
        {SpyBountyDifficulty.Medium, 0.5f},
        {SpyBountyDifficulty.Hard, 1f},
    };

    [DataField]
    public HashSet<MapId> StationMaps = new();
}
