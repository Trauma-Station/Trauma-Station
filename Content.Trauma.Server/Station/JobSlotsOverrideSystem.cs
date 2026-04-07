// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Content.Trauma.Shared.Station;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Station;

/// <summary>
/// Changes the limit for jobs regardless of the map chosen if pop is within a range.
/// Uses <see cref="JobSlotsOverridePrototype"/> prototypes defined in yml.
/// </summary>
public sealed class JobSlotsOverrideSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationJobsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StationJobsComponent> ent, ref MapInitEvent args)
    {
        if (GetSlotsOverride() is not {} proto)
            return; // lowpop dont care

        // everything uses this so modify it first
        foreach (var (job, slots) in proto.Jobs)
        {
            ent.Comp.SetupAvailableJobs[job] = [ slots, slots ];
        }

        // this is needed for latejoin etc, normally created on ComponentStartup for StationData... so have to do it again now
        ent.Comp.JobList.Clear();
        ent.Comp.TotalJobs = 0;
        foreach (var (job, slots) in ent.Comp.SetupAvailableJobs)
        {
            int? n = slots[1] < 0 ? null : slots[1];
            ent.Comp.JobList[job] = n;
            ent.Comp.TotalJobs += n ?? 0;
        }

        _stationJobs.UpdateJobsAvailable();

        // this is completely unused but just futureproofing incase someone needs it later
        ent.Comp.MidRoundTotalJobs = 0;
        foreach (var slots in ent.Comp.SetupAvailableJobs.Values)
        {
            ent.Comp.MidRoundTotalJobs += Math.Max(slots[1], 0);
        }

        // this is still important though
        var overflow = new HashSet<ProtoId<JobPrototype>>();
        foreach (var (job, slots) in ent.Comp.SetupAvailableJobs)
        {
            if (slots[1] < 0)
                overflow.Add(job);
        }
        ent.Comp.OverflowJobs = overflow;
    }

    /// <summary>
    /// Returns a job slots override for the current player count.
    /// </summary>
    /// <remarks>
    /// If multiple prototypes have overlapping ranges, which one gets returned is unreliable.
    /// So just don't do that.
    /// </remarks>
    private JobSlotsOverridePrototype? GetSlotsOverride()
    {
        int pop = _player.PlayerCount;
        foreach (var proto in _proto.EnumeratePrototypes<JobSlotsOverridePrototype>())
        {
            if (proto.Min is {} min && pop < min || proto.Max is {} max && pop > max)
                continue;

            return proto;
        }

        return null;
    }
}
