// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Throwing;
using Content.Trauma.Common.Throwing;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class PreventPushStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, BeingThrownAttemptEvent>(_statusEffects.RelayEvent);

        SubscribeLocalEvent<PreventPushStatusEffectComponent, StatusEffectRelayedEvent<BeingThrownAttemptEvent>>(OnPush);
    }

    private void OnPush(Entity<PreventPushStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeingThrownAttemptEvent> args)
    {
        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }
}
