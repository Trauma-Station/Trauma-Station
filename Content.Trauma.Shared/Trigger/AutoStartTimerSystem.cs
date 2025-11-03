using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;

namespace Content.Trauma.Shared.Trigger;

public sealed class AutoStartTimerSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoStartTimerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AutoStartTimerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<TimerTriggerComponent>(ent, out var timer))
            return;

        _trigger.ActivateTimerTrigger((ent.Owner, timer));
    }
}
