// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Trauma.Shared.Ranching.Components;
using Content.Trauma.Shared.Ranching.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Ranching;

/// <summary>
/// This handles raising the egg layer event on the chicken when it should lay an egg.
/// </summary>
public sealed partial class RanchingEggLayerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery = default!;

    private List<Entity<RanchingEggLayerComponent>> toLayEgg = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<RanchingEggLayerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextGrowth = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.EggLayCooldownMin, ent.Comp.EggLayCooldownMax));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        toLayEgg.Clear();

        var query = EntityQueryEnumerator<RanchingEggLayerComponent>();
        while (query.MoveNext(out var uid, out var eggLayer))
        {
            if (_mobState.IsDead(uid) || _mobState.IsCritical(uid))
                continue;

            if (_timing.CurTime < eggLayer.NextGrowth)
                continue;

            eggLayer.NextGrowth += TimeSpan.FromSeconds(_random.NextFloat(eggLayer.EggLayCooldownMin, eggLayer.EggLayCooldownMax));

            toLayEgg.Add((uid, eggLayer));
        }

        foreach (var (uid, eggLayer) in toLayEgg)
        {
            TryLayEgg(uid, eggLayer);
        }
    }

    public void TryLayEgg(EntityUid uid, RanchingEggLayerComponent? egglayer)
    {
        if (!Resolve(uid, ref egglayer))
            return;

        if (!_satiationQuery.TryComp(uid, out var satiation))
            return;

        if (!_hunger.IsValueInRange((uid, satiation), SatiationSystem.Hunger, above: egglayer.HungerThreshold))
            return;

        var evfood = new RanchingEggLayAttemptEvent((uid, egglayer));
        RaiseLocalEvent(uid, ref evfood);
    }
}
