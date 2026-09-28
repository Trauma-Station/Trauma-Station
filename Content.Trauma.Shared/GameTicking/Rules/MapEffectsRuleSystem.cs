// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;

namespace Content.Trauma.Shared.GameTicking.Rules;

public sealed partial class MapEffectsRuleSystem : GameRuleSystem<MapEffectsRuleComponent>
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private StationSystem _station = default!;

    protected override void Started(Entity<MapEffectsRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        if (GetFirstStation() is not {} station ||
            _station.GetLargestGrid(station) is not {} grid ||
            Transform(grid).MapUid is not {} map)
            return;

        _effects.ApplyEffects(map, ent.Comp1.Effects, predicted: false); // they probably arent predicted yet
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
