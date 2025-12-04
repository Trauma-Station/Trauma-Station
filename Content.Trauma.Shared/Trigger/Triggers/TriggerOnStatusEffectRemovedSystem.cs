// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger.Systems;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed class TriggerOnStatusEffectRemovedSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnStatusEffectRemovedComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnRemoved(Entity<TriggerOnStatusEffectRemovedComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _trigger.Trigger(ent, user: args.Target, key: ent.Comp.KeyOut);
    }
}
