// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Flash;
using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Trigger;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed class TriggerOnFlashedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnFlashedComponent, AfterFlashedEvent>(OnFlashed);
    }

    private void OnFlashed(Entity<TriggerOnFlashedComponent> ent, ref AfterFlashedEvent args)
    {
        var tick = (int) _timing.CurTick.Value;
        var seed = SharedRandomExtensions.HashCodeCombine(tick, GetNetEntity(ent).Id);
        var rand = new Random(seed);
        if (rand.Prob(ent.Comp.Prob))
            _trigger.Trigger(ent, args.User, ent.Comp.KeyOut);
    }
}
