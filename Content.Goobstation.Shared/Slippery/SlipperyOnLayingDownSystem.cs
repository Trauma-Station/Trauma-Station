// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Slippery;
using Content.Shared.Standing;
using Content.Shared.StepTrigger.Components;

namespace Content.Goobstation.Shared.Slippery;

/// <summary>
/// Causes the person given this to gain
/// Slippery and StepTrigger when they're laying down.
/// </summary>
public sealed class SlipperyOnLayingDownSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipperyOnLayingDownComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<SlipperyOnLayingDownComponent, StoodEvent>(OnStood);
    }

    private void OnDowned(Entity<SlipperyOnLayingDownComponent> uid, ref DownedEvent args)
    {
        EnsureComp<SlipperyComponent>(uid);
        EnsureComp<StepTriggerComponent>(uid);
    }

    private void OnStood(Entity<SlipperyOnLayingDownComponent> uid, ref StoodEvent args)
    {
        RemComp<SlipperyComponent>(uid);
        RemComp<StepTriggerComponent>(uid);
    }
}
