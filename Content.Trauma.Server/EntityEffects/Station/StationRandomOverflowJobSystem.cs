// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Station;
using Robust.Shared.Random;

namespace Content.Trauma.Server.EntityEffects.Station;

public sealed class StationRandomOverflowJobSystem : EntityEffectSystem<StationJobsComponent, StationRandomOverflowJob>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    protected override void Effect(Entity<StationJobsComponent> ent, ref EntityEffectEvent<StationRandomOverflowJob> args)
    {
        var ignored = args.Effect.IgnoredJobs;
        var jobs = new List<string>();
        foreach (var job in _stationJobs.GetJobs(ent).Keys)
        {
            if (ignored.Contains(job) ||
                // don't be boring and change nothing
                _stationJobs.IsJobUnlimited(ent, job, ent.Comp))
                continue;

            jobs.Add(job);
        }

        if (jobs.Count == 0)
            return;

        var picked = _random.Pick(jobs);
        _stationJobs.MakeJobUnlimited(ent, picked, ent.Comp);
    }
}
