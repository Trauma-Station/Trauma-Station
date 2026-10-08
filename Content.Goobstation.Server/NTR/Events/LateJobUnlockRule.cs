// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Shared.Station.Components;

namespace Content.Goobstation.Server.NTR.Events;

public sealed partial class LateJobUnlockRule : StationEventSystem<LateJobUnlockRuleComponent>
{
    [Dependency] private ServerStationJobsSystem _stationJobs = default!;

    protected override void Started(Entity<LateJobUnlockRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var comp = ent.Comp1;
        foreach (var station in Station.GetStationsSet())
        {
            if (!HasComp<StationJobsComponent>(station))
                continue;

            foreach (var (jobProtoId, slotCount) in comp.JobsToAdd)
            {
                var jobId = jobProtoId.ToString();

                if (!ProtoMan.HasIndex<JobPrototype>(jobProtoId))
                {
                    Log.Error($"Job prototype '{jobId}' not found for station {ToPrettyString(station)}");
                    continue;
                }

                var currentSlots = _stationJobs.TryGetJobSlot(station, jobId, out var slots) ? slots ?? 0 : 0;
                _stationJobs.TrySetJobSlot(station, jobId, currentSlots + slotCount);
            }
        }
    }
}
