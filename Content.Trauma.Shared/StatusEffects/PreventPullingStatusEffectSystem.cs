// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Pulling.Events;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class PreventPullingStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, BeingPulledAttemptEvent>(_statusEffects.RelayStatusEffectEvent);

        SubscribeLocalEvent<PreventPullingStatusEffectComponent, StatusEffectRelayedEvent<BeingPulledAttemptEvent>>(OnPulled);
    }

    private void OnPulled(Entity<PreventPullingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeingPulledAttemptEvent> args)
    {
        var ev = args.Args;
        ev.Cancel();
        args.Args = ev;
    }
}
