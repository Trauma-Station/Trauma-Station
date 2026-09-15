// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind.Toolshed;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
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
        var existingSideJobs = GetExistingSideJobs(ent);
        OnFetch(ent.Comp.Protos, args.SideJobs, args.Mind, args.EffectiveLevel, existingSideJobs);
        OnFetch(ent.Comp.PriorityProtos, args.PrioritySideJobs, args.Mind, args.EffectiveLevel, existingSideJobs);
    }

    private void OnFetch(List<EntProtoId<SideJobComponent>> protos, List<EntityUid> output, Entity<MindComponent> mind, int effectiveLevel, List<EntityUid> existingSideJobs)
    {
        foreach (var id in protos)
        {
            // check for duplicates (any pre-existing side job with matching prototype id)
            var duplicates = false;
            foreach (var otherSideJob in existingSideJobs)
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
            if (!InitializeSideJob(sideJob, mind, effectiveLevel))
                continue;

            output.Add(sideJob);
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
