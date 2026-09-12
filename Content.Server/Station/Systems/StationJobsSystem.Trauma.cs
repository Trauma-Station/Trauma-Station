using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private EntityQuery<StationDataComponent> _stationQuery = default!;

    private Dictionary<ProtoId<JobPrototype>, int> GetRequiredJobs(EntityUid station)
    {
        var id = _stationQuery.CompOrNull(station)?.JobWeights ?? JobWeightPrototype.Default;
        return ProtoMan.Index(id).Required;
    }
}
