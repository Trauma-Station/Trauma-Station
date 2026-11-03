// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.StationEvents.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.StationEvents.Events;

public sealed partial class JobDistributionErrorRule : StationEventSystem<JobDistributionErrorRuleComponent>
{
    [Dependency] private ServerStationJobsSystem _stationJobs = default!;

    protected override void Started(Entity<JobDistributionErrorRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation(out var chosenStation, HasComp<StationJobsComponent>))
            return;

        var comp = ent.Comp1;
        var rand = RobustRandom;
        int jobsAdded = rand.Next(comp.MinJobs, comp.MaxJobs);

        for (int i = 0; i < jobsAdded; i++)
        {
            int slotsAdded = rand.Next(comp.MinAmount, comp.MaxAmount);
            var chosenJob = rand.PickAndTake(comp.Jobs);

            _stationJobs.TryAdjustJobSlot(chosenStation.Value, chosenJob, slotsAdded, true, true);
        }
    }
}
