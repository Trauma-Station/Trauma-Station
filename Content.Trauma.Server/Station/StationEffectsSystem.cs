// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Station;

namespace Content.Trauma.Server.Station;

public sealed partial class StationEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private SharedMapSystem _map = default!;

    [SubscribeLocalEvent]
    private void OnStationPostInit(Entity<StationEffectsComponent> ent, ref StationPostInitEvent args)
    {
        var station = _station.GetLargestGrid(ent.Owner);
        if (station == null)
            return;

        var mapId = Transform(station.Value).MapID;
        var mapUid = _map.GetMapOrInvalid(mapId);

        _effects.ApplyEffects(mapUid, ent.Comp.Effects);
    }
}
