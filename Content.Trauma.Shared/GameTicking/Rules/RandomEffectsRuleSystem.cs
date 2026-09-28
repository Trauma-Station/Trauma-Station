// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Station.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.GameTicking.Rules;

public sealed partial class RandomEffectsRuleSystem : GameRuleSystem<RandomEffectsRuleComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Added(Entity<RandomEffectsRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        if (GetFirstStation() is not {} station)
        {
            GameTicker.EndGameRule((ent, ent.Comp2));
            return;
        }

        ent.Comp1.Station = station;
    }

    protected override void Started(Entity<RandomEffectsRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        SetCooldown(ent.Comp1);
    }

    protected override void ActiveTick(EntityUid uid, RandomEffectsRuleComponent comp, GameRuleComponent rule, float frameTime)
    {
        base.ActiveTick(uid, comp, rule, frameTime);

        if (_timing.CurTime < comp.NextEffect)
            return;

        SetCooldown(comp);
        _effects.ApplyEffects(comp.Station, comp.Effects, predicted: false);
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
