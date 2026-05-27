// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class RandomEffectsRuleSystem : GameRuleSystem<RandomEffectsRuleComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Added(EntityUid uid, RandomEffectsRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (GetFirstStation() is not {} station)
        {
            GameTicker.EndGameRule(uid, gameRule);
            return;
        }

        comp.Station = station;
    }

    protected override void Started(EntityUid uid, RandomEffectsRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        SetCooldown(comp);
    }

    protected override void ActiveTick(EntityUid uid, RandomEffectsRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        if (_timing.CurTime < comp.NextEffect)
            return;

        SetCooldown(comp);
        _effects.ApplyEffects(comp.Station, comp.Effects);
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

    private void SetCooldown(RandomEffectsRuleComponent comp)
    {
        var min = comp.MinTime.TotalSeconds;
        var max = comp.MaxTime.TotalSeconds;
        var seconds = _random.NextDouble(min, max);
        comp.NextEffect = _timing.CurTime + TimeSpan.FromSeconds(seconds);
    }
}
