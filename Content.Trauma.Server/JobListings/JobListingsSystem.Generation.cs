// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Random.Helpers;
using Content.Trauma.Shared.JobListings;
using Robust.Shared.Random;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the generation of sidejobs by subscribing to the <see cref=SideJobCreatedEvent/>.
/// </summary>
public sealed partial class JobListingsSystem
{
    [SubscribeLocalEvent]
    private void OnCreatedWithReward(Entity<GenerateSideJobRewardComponent> ent, ref SideJobCreatedEvent args)
    {
        if (!TryComp<SideJobComponent>(ent, out var sideJobComp))
        {
            args.Cancelled = true;
            return;
        }

        var roll = _random.NextFloat();
        if (roll <= ent.Comp.CurrencyChance)
        {
            sideJobComp.Reward = ent.Comp.CurrencyReward;
            sideJobComp.RewardName = ent.Comp.CurrencyName;
        }
        else
        {
            var index = _random.Next(ent.Comp.UplinkRewards.Count);
            var entryId = ent.Comp.UplinkRewards[index];
            var entry = ProtoMan.Index(entryId);

            if (entry.ProductEntity is not { } reward || entry.Name is not { } name)
            {
                args.Cancelled = true;
                return;
            }

            sideJobComp.Reward = reward;
            sideJobComp.RewardName = name;
        }

        DirtyFields(ent, sideJobComp, null, [nameof(SideJobComponent.Reward), nameof(SideJobComponent.RewardName)]);
    }

    [SubscribeLocalEvent]
    private void OnCreatedWithLevelRestriction(Entity<LevelRestrictedSideJobComponent> ent, ref SideJobCreatedEvent args)
    {
        if (args.EffectiveLevel != ent.Comp.Level)
            args.Cancelled = true;
    }
}
