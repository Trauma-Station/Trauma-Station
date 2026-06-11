// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the reward (and tool) generation and retrieval for side jobs.
/// </summary>
public sealed partial class JobListingsSystem
{
    private void InitializeReward()
    {
        SubscribeLocalEvent<GenerateSideJobRewardComponent, SideJobCreatedEvent>(OnSideJobCreated);
    }

    private void OnSideJobCreated(Entity<GenerateSideJobRewardComponent> ent, ref SideJobCreatedEvent args)
    {
        if (!_proto.Resolve(ent.Comp.RewardTable, out var table))
        {
            args.Cancelled = true;
            return;
        }

        var reward = _table.GetSpawns(table).FirstOrNull();
        if (reward is null || !TryComp<SideJobComponent>(ent.Owner, out var sideJobComp))
        {
            args.Cancelled = true;
            return;
        }

        sideJobComp.Reward = reward.Value;
    }
}
