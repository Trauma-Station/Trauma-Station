// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Station.Systems;

namespace Content.Trauma.Server.Ghost;

public sealed partial class TraumaGhostRoleSystem : EntitySystem
{
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    // Use body init event so that skill chip organ special actually manages to find brain
    [SubscribeLocalEvent]
    private void OnInit(Entity<GhostRoleComponent> ent, ref BodyInitEvent args)
    {
        if (ent.Comp.JobProto is not { } job)
            return;

        _stationSpawning.DoJobSpecials(job, ent);
    }
}
