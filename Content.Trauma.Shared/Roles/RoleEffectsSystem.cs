// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Roles;

namespace Content.Trauma.Shared.Roles;

public sealed partial class RoleEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnAdded(Entity<RoleEffectsComponent> ent, ref RoleGotAddedEvent args)
    {
        // TODO: add predicted bools to the mind role chain...
        _effects.ApplyEffects(args.Mind, ent.Comp.MindAdded, predicted: false);
        if (args.Mob is not {} mob)
            return;

        _effects.ApplyEffects(mob, ent.Comp.Added, predicted: false);
        if (ent.Comp.SingleUse)
            RemCompDeferred(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<RoleEffectsComponent> ent, ref RoleGotRemovedEvent args)
    {
        _effects.ApplyEffects(args.Mind, ent.Comp.MindRemoved, predicted: false);
        if (args.Mob is not {} mob)
            return;

        _effects.ApplyEffects(mob, ent.Comp.Removed, predicted: false);
    }

    [SubscribeLocalEvent]
    private void OnMindAdded(Entity<RoleEffectsComponent> ent, ref RoleMindAddedEvent args)
    {
        _effects.ApplyEffects(args.Mob, ent.Comp.Added, predicted: false);
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(Entity<RoleEffectsComponent> ent, ref RoleMindRemovedEvent args)
    {
        _effects.ApplyEffects(args.Mob, ent.Comp.Removed);
    }
}
