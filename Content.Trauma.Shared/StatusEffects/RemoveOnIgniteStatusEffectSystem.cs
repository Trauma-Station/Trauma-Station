// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class RemoveOnIgniteStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, IgnitedEvent>(_status.RelayEvent);

        SubscribeLocalEvent<RemoveOnIgniteStatusEffectComponent, StatusEffectRelayedEvent<IgnitedEvent>>(OnIgnite);
    }

    private void OnIgnite(Entity<RemoveOnIgniteStatusEffectComponent> ent, ref StatusEffectRelayedEvent<IgnitedEvent> args)
    {
        _status.TryRemoveStatusEffect(args.Container.Owner, ent.Comp.EffectProto);
    }
}
