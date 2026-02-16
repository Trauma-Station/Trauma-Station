// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Trigger;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed class RandomTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTriggerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var tick = (int) _timing.CurTick.Value;
        var query = EntityQueryEnumerator<RandomTriggerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            var seed = SharedRandomExtensions.HashCodeCombine(tick, GetNetEntity(uid).Id);
            var rand = new Random(seed);
            if (!rand.Prob(comp.Prob))
                continue;

            _trigger.Trigger(uid, key: comp.KeyOut);
        }
    }

    private void OnMapInit(Entity<RandomTriggerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;
    }
}
