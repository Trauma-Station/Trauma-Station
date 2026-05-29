// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Station;

namespace Content.Trauma.Server.EntityEffects.Station;

public sealed partial class StationModifyJobsSystem : EntityEffectSystem<StationJobsComponent, StationModifyJobs>
{
    [Dependency] private StationJobsSystem _stationJobs = default!;

    protected override void Effect(Entity<StationJobsComponent> ent, ref EntityEffectEvent<StationModifyJobs> args)
    {
        foreach (var (job, add) in args.Effect.Add)
        {
            _stationJobs.TryAdjustJobSlot(ent, job, add, stationJobs: ent.Comp);
        }

        foreach (var (job, value) in args.Effect.Set)
        {
            _stationJobs.TrySetJobSlot(ent, job, value, stationJobs: ent.Comp);
        }
    }
}
