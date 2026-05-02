// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// This handles...
/// </summary>
public sealed class RemoveOnIgniteStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, IgnitedEvent>(_status.RelayEvent);

        SubscribeLocalEvent<RemoveOnIgniteStatusEffectComponent, StatusEffectRelayedEvent<IgnitedEvent>>(OnIgnite);

        SubscribeLocalEvent<RemoveOnIgniteStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<RemoveOnIgniteStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
    }

    private void OnIgnite(Entity<RemoveOnIgniteStatusEffectComponent> ent, ref StatusEffectRelayedEvent<IgnitedEvent> args)
    {
        if (ent.Comp.StatusOwner is not { } target)
            return;

        _status.TryRemoveStatusEffect(target, ent.Comp.EffectProto);
    }

    private void OnApplied(Entity<RemoveOnIgniteStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.StatusOwner = args.Target;
        Dirty(ent);
    }

    private void OnRemove(Entity<RemoveOnIgniteStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.StatusOwner = null;
        Dirty(ent);
    }
}
