// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Handles running effects for <see cref="EffectsMutationComponent"/>.
/// </summary>
public sealed class EffectsMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectsMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<EffectsMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<EffectsMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (args.Automatic && ent.Comp.IgnoreAutomatic)
            return;

        _effects.ApplyEffects(args.Target, ent.Comp.Added, user: args.User);
    }

    private void OnRemoved(Entity<EffectsMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (args.Automatic && ent.Comp.IgnoreAutomatic)
            return;

        _effects.ApplyEffects(args.Target, ent.Comp.Removed, user: args.User);
    }
}
