// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Station;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.EntityEffects.Station;

public sealed class StationModifyJobsSystem : EntityEffectSystem<StationJobsComponent, StationModifyJobs>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    protected override void Effect(Entity<StationJobsComponent> ent, ref EntityEffectEvent<StationModifyJobs> args)
    {
        foreach (var (job, add) in args.Effect.Add)
        {
            _stationJobs.TryAdjustJobSlot(ent, _proto.Index(job), add, stationJobs: ent.Comp);
        }
    }
}
