// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Content.Trauma.Shared.EntityEffects.Station;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.EntityEffects.Station;

// TODO: move to shared if station jobs system gets a shared api
public sealed partial class StationRandomOverflowJobSystem : EntityEffectSystem<StationJobsComponent, StationRandomOverflowJob>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ServerStationJobsSystem _stationJobs = default!;

    protected override void Effect(Entity<StationJobsComponent> ent, ref EntityEffectEvent<StationRandomOverflowJob> args)
    {
        var ignored = args.Effect.IgnoredJobs;
        var jobs = new List<string>();
        foreach (var (job, slots) in ent.Comp.JobList)
        {
            if (ignored.Contains(job) ||
                // don't be boring and change nothing for jobs that are already unlimited
                slots == null)
                continue;

            jobs.Add(job);
        }

        if (jobs.Count == 0)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var picked = rand.Pick(jobs);
        _stationJobs.MakeJobUnlimited(ent, picked, ent.Comp);
    }
}
