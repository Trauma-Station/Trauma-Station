// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Spy;

/// <summary>
/// Gamerule comp for spy antag
/// This one is in shared and is networked for all spies for convenience
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
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

    /// <summary>
    /// Bounties that cannot be rolled due to item not existing on station or other reasons
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpyBountyPrototype>> UnavailableBounties = new();

    /// <summary>
    /// Bounties that were already claimed by some spy and cannot be rolled again
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpyBountyPrototype>> ClaimedBounties = new();

    /// <summary>
    /// All bounties that can be selected for spies.
    /// Old bounties do not repeat in bounty least unless they are marked as Repeatable.
    /// If bounty pool depletes, it is filled again with all possible bounties
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpyBountyPrototype>, float>? BountyPool;

    /// <summary>
    /// Currently selected bounties that spies can claim
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<SpyBounty> CurrentBounties = new();

    /// <summary>
    /// Time until bounties are refreshed
    /// </summary>
    [DataField]
    public TimeSpan RefreshTime = TimeSpan.FromMinutes(12);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextRefresh = TimeSpan.Zero;

    /// <summary>
    /// All loot that spies can roll from their bounty, based on difficulty
    /// Difficulty -> ([ListingPrototype.ID OR SpyRewardPrototype.ID] -> weight)
    /// </summary>
    [DataField]
    public Dictionary<SpyBountyDifficulty, Dictionary<string, float>> LootPool = new();

    /// <summary>
    /// Rewards for currently selected bounties,
    /// used for updating <see cref="LootPool"/> on bounty refresh and avoiding duplicate rewards when refreshing
    /// </summary>
    [DataField]
    public Dictionary<SpyBountyDifficulty, Dictionary<string, float>> CachedRewards = new();

    /// <summary>
    /// Bounty difficulty is based on the amount of TC cost of the reward
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, SpyBountyDifficulty> CostToDifficulty = new()
    {
        {0, SpyBountyDifficulty.Easy},
        {30, SpyBountyDifficulty.Medium},
        {60, SpyBountyDifficulty.Hard},
    };

    /// <summary>
    /// Chance that reward will be removed from <see cref="LootPool"/> when claimed by someone, based on difficulty
    /// </summary>
    [DataField]
    public Dictionary<SpyBountyDifficulty, float> ChancesToRemoveRewardFromPool = new()
    {
        {SpyBountyDifficulty.Easy, 0.25f},
        {SpyBountyDifficulty.Medium, 0.5f},
        {SpyBountyDifficulty.Hard, 1f},
    };

    /// <summary>
    /// Used for checking if item exists on the station, needed for some bounties
    /// </summary>
    [DataField]
    public HashSet<MapId> StationMaps = new();
}
