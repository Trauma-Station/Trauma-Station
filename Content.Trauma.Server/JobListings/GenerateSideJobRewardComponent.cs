// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;
using Content.Shared.Store;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Generates a reward for a side job by pulling a prototype from a table.
/// </summary>
[RegisterComponent]
public sealed partial class GenerateSideJobRewardComponent : Component
{
    /// <summary>
    /// A prototype for telecrystals as the main reward.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId CurrencyReward;

    /// <summary>
    /// The chance that the reward is currency. If it is not then an item is taken from a list of uplink entries.
    /// </summary>
    [DataField(required: true)]
    public float CurrencyChance;

    /// <summary>
    /// The real name of the currency, including a number.
    /// The entity name will always just be 'telecrystals' and you would not know how much you are getting.
    /// </summary>
    [DataField(required: true)]
    public string CurrencyName;

    /// <summary>
    /// A list of uplink entries for an alternative reward.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ListingPrototype>> UplinkRewards;
}
