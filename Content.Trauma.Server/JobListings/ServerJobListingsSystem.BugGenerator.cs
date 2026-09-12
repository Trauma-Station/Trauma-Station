// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the <see cref="BugSideJobGeneratorComponent"/>.
/// </summary>
public sealed partial class ServerJobListingsSystem
{
    [SubscribeLocalEvent]
    private void OnFetch(Entity<BugSideJobGeneratorComponent> ent, ref GenerateSideJobsEvent args)
    {
        foreach (var area in ent.Comp.Areas)
        {
            // check for duplicates (any pre-existing side job with matching prototype id)
            var duplicates = false;
            foreach (var otherSideJob in GetExistingSideJobs(ent))
            {
                if (TryComp<BugAreaConditionComponent>(otherSideJob, out var bugAreaComp))
                {
                    if (bugAreaComp.TargetArea == area)
                    {
                        duplicates = true;
                        continue;
                    }
                }
            }

            if (duplicates)
                continue;

            // spawn and initialise
            var sideJob = Spawn(ent.Comp.Proto);

            if (!TryComp<BugAreaConditionComponent>(sideJob, out var bugAreaComp2))
            {
                Log.Error($"Side Job {sideJob} spawned without BugAreaConditionComponent despite being the Proto field for BugSideJobGeneratorComponent.");
                Del(sideJob);
                continue;
            }
            bugAreaComp2.TargetArea = area;

            if (!InitializeSideJob(sideJob, args.Mind, args.EffectiveLevel))
                continue;

            args.SideJobs.Add(sideJob);
        }
    }

    [SubscribeLocalEvent]
    private void OnClaim(Entity<BugSideJobGeneratorComponent> ent, ref SideJobClaimedEvent args)
    {
        if (!TryComp<BugAreaConditionComponent>(args.SideJob, out var bugAreaComp) || bugAreaComp.TargetArea is not { } area)
            return;

        ent.Comp.Areas.Remove(area);
    }
}
