using Content.Shared.Buckle.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Shared.Buckle;

public sealed class StrapEntityEffectsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrapEntityEffectsComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<ActiveStrapEntityEffectsComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<StrapEntityEffectsComponent> ent, ref StrappedEvent args)
    {
        var active = EnsureComp<ActiveStrapEntityEffectsComponent>(ent);
        Update((ent.Owner, ent.Comp, active), args.Buckle);
    }

    private void OnUnstrapped(Entity<ActiveStrapEntityEffectsComponent> ent, ref UnstrappedEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveStrapEntityEffectsComponent, StrapEntityEffectsComponent, StrapComponent>();
        while (query.MoveNext(out var uid, out var active, out var comp, out var strap))
        {
            if (now < active.NextUpdate)
                continue;

            // shouldn't happen because of UnstrappedEvent
            var buckled = strap.BuckledEntities.FirstOrDefault();
            if (buckled == default!)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            Update((uid, comp, active), buckled);
        }
    }

    public void Update(Entity<StrapEntityEffectsComponent, ActiveStrapEntityEffectsComponent> ent, EntityUid target)
    {
        _effects.ApplyEffects(target, ent.Comp1.Effects);
        ent.Comp2.NextUpdate = _timing.CurTime + ent.Comp1.UpdateDelay;
        Dirty(ent.Owner, ent.Comp2);
    }
}
