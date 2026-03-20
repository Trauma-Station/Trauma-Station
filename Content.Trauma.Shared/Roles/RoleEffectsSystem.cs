// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Roles;

namespace Content.Trauma.Shared.Roles;

public sealed class RoleEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleEffectsComponent, RoleGotAddedEvent>(OnAdded);
        SubscribeLocalEvent<RoleEffectsComponent, RoleGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<RoleEffectsComponent, RoleMindAddedEvent>(OnMindAdded);
        SubscribeLocalEvent<RoleEffectsComponent, RoleMindRemovedEvent>(OnMindRemoved);
    }

    private void OnAdded(Entity<RoleEffectsComponent> ent, ref RoleGotAddedEvent args)
    {
        _effects.ApplyEffects(args.Mind, ent.Comp.MindAdded);
        if (args.Mob is not {} mob)
            return;

        _effects.ApplyEffects(mob, ent.Comp.Added);
    }

    private void OnRemoved(Entity<RoleEffectsComponent> ent, ref RoleGotRemovedEvent args)
    {
        _effects.ApplyEffects(args.Mind, ent.Comp.MindRemoved);
        if (args.Mob is not {} mob)
            return;

        _effects.ApplyEffects(mob, ent.Comp.Removed);
    }

    private void OnMindAdded(Entity<RoleEffectsComponent> ent, ref RoleMindAddedEvent args)
    {
        _effects.ApplyEffects(args.Mob, ent.Comp.Added);
    }

    private void OnMindRemoved(Entity<RoleEffectsComponent> ent, ref RoleMindRemovedEvent args)
    {
        _effects.ApplyEffects(args.Mob, ent.Comp.Removed);
    }
}
