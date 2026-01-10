// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Body.Part;

public sealed class TriggerInsideBodyPartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerInsideBodyPartComponent, InsertedIntoCavityEvent>(OnInsertedIntoCavity);
        SubscribeLocalEvent<TriggerInsideBodyPartComponent, RemovedFromCavityEvent>(OnRemovedFromCavity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveTriggerInsideBodyPartComponent, TriggerInsideBodyPartComponent>();
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (now < active.NextTrigger)
                continue;

            _trigger.Trigger(uid, key: comp.KeyOut);
            RemCompDeferred(uid, active);
            RemCompDeferred(uid, comp);
        }
    }

    private void OnInsertedIntoCavity(Entity<TriggerInsideBodyPartComponent> ent, ref InsertedIntoCavityEvent args)
    {
        if (ent.Comp.Delay == TimeSpan.Zero)
        {
            _trigger.Trigger(ent.Owner, key: ent.Comp.KeyOut);
            return;
        }

        var active = EnsureComp<ActiveTriggerInsideBodyPartComponent>(ent);
        active.NextTrigger = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent, active);
    }

    private void OnRemovedFromCavity(Entity<TriggerInsideBodyPartComponent> ent, ref RemovedFromCavityEvent args)
    {
        RemComp<ActiveTriggerInsideBodyPartComponent>(ent);
    }
}
