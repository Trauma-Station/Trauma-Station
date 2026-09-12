// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the <see cref="BasicSideJobGeneratorComponent"/>.
/// </summary>
public sealed partial class ServerJobListingsSystem
{
    [SubscribeLocalEvent]
    private void OnFetch(Entity<BasicSideJobGeneratorComponent> ent, ref GenerateSideJobsEvent args)
    {
        foreach (var id in ent.Comp.Protos)
        {
            // check for duplicates (any pre-existing side job with matching prototype id)
            var duplicates = false;
            foreach (var otherSideJob in GetExistingSideJobs(ent))
            {
                var proto = Prototype(otherSideJob);
                if (proto is not null && proto.ID == id)
                {
                    duplicates = true;
                    continue;
                }
            }

            if (duplicates)
                continue;

            // spawn and initialise
            var sideJob = Spawn(id);
            if (!InitializeSideJob(sideJob, args.Mind, args.EffectiveLevel))
                continue;

            // put in priority as per the component's doc comment
            args.PrioritySideJobs.Add(sideJob);
        }
    }

    [SubscribeLocalEvent]
    private void OnClaim(Entity<BasicSideJobGeneratorComponent> ent, ref SideJobClaimedEvent args)
    {
        if (Prototype(args.SideJob) is not { } proto)
            return;

        ent.Comp.Protos.Remove(proto.ID);
    }
}
