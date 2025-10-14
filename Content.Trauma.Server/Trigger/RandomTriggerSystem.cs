using Content.Server.Explosion.EntitySystems;
using Content.Trauma.Shared.Trigger;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Trigger;

public sealed class RandomTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!; // TODO: update if trigger refactor is cherry picked

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTriggerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RandomTriggerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + comp.UpdateDelay;
            if (!_random.Prob(comp.Prob))
                continue;

            _trigger.Trigger(uid);
        }
    }

    private void OnMapInit(Entity<RandomTriggerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;
    }
}
