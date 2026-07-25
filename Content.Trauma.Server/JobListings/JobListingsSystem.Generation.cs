// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the generation of sidejobs by subscribing to the <see cref=SideJobCreatedEvent/>.
/// </summary>
public sealed partial class JobListingsSystem
{
    private void InitializeReward()
    {
        SubscribeLocalEvent<GenerateSideJobRewardComponent, SideJobCreatedEvent>(OnCreatedWithReward);
        SubscribeLocalEvent<LevelRestrictedSideJobComponent, SideJobCreatedEvent>(OnCreatedWithLevelRestriction);
    }

    private void OnCreatedWithReward(Entity<GenerateSideJobRewardComponent> ent, ref SideJobCreatedEvent args)
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

    private void OnCreatedWithLevelRestriction(Entity<LevelRestrictedSideJobComponent> ent, ref SideJobCreatedEvent args)
    {
        if (args.EffectiveLevel != ent.Comp.Level)
            args.Cancelled = true;

    }
}
