// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Trauma.Server.GameTicking.Rules.Components;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class MapEffectsRuleSystem : GameRuleSystem<MapEffectsRuleComponent>
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedStationSystem _station = default!;

    protected override void Started(EntityUid uid, MapEffectsRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (GetFirstStation() is not {} station ||
            _station.GetLargestGrid(station) is not {} grid ||
            Transform(grid).MapUid is not {} map)
            return;

        _effects.ApplyEffects(map, comp.Effects);
    }

    private EntityUid? GetFirstStation()
    {
        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            return uid;
        }

        return null;
    }
}
