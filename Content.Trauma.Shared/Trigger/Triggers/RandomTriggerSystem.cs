// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Trigger;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed partial class RandomTriggerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    private List<Entity<RandomTriggerComponent>> _triggering = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTriggerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RandomTriggerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            if (!SharedRandomExtensions.PredictedProb(_timing, comp.Prob, GetNetEntity(uid)))
                continue;

            // wait until outside the query to trigger incase it spawns/deletes a RandomTrigger
            _triggering.Add((uid, comp));
        }

        foreach (var ent in _triggering)
        {
            _trigger.Trigger(ent.Owner, key: ent.Comp.KeyOut);
        }
        _triggering.Clear();
    }

    private void OnMapInit(Entity<RandomTriggerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;
    }
}
