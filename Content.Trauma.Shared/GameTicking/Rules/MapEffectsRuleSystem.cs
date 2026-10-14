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
        if (!_station.TryGetRandomStation(out var station) ||
            _station.GetStationGridUid(station.Value) is not {} grid ||
            Transform(grid).MapUid is not {} map)
            return;

        _effects.ApplyEffects(map, ent.Comp1.Effects, predicted: false); // they probably arent predicted yet
    }
}
