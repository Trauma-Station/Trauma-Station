// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;

namespace Content.Trauma.Shared.Trigger;

public sealed partial class AutoStartTimerSystem : EntitySystem
{
    [Dependency] private TriggerSystem _trigger = default!;

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
