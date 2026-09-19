// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the <see cref="StealSideJobGeneratorComponent"/>.
/// </summary>
public sealed partial class ServerJobListingsSystem
{
    [Dependency] private StealConditionSystem _steal = default!;

    [SubscribeLocalEvent]
    private void OnFetch(Entity<StealSideJobGeneratorComponent> ent, ref GenerateSideJobsEvent args)
    {
        foreach (var target in ent.Comp.Targets)
        {
            // check for duplicates (any pre-existing side job with matching prototype id)
            var duplicates = false;
            foreach (var otherSideJob in GetExistingSideJobs(ent))
            {
                if (TryComp<StealConditionComponent>(otherSideJob, out var stealComp))
                {
                    if (stealComp.StealGroup == target)
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

            if (!TryComp<StealConditionComponent>(sideJob, out var stealComp2))
            {
                Log.Error($"Side Job {sideJob} spawned without StealConditionComponent despite being the Proto field for StealSideJobGeneratorComponent.");
                Del(sideJob);
                continue;
            }
            _steal.SetStealGroup((sideJob, stealComp2), target);

            if (!InitializeSideJob(sideJob, args.Mind, args.EffectiveLevel))
                continue;

            args.SideJobs.Add(sideJob);
        }
    }

    [SubscribeLocalEvent]
    private void OnClaim(Entity<StealSideJobGeneratorComponent> ent, ref SideJobClaimedEvent args)
    {
        if (!TryComp<StealConditionComponent>(args.SideJob, out var stealComp) || stealComp.StealGroup is not { } groupId)
            return;

        ent.Comp.Targets.Remove(groupId);
    }
}
