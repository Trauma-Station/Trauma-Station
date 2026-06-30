// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Generates a reward for a side job by pulling a prototype from a table.
/// </summary>
[RegisterComponent]
public sealed partial class GenerateSideJobRewardComponent : Component
{
    /// <summary>
    /// A table to pick the reward from.
    /// </summary>
    [DataField]
    public ProtoId<EntityTablePrototype> RewardTable;
}
