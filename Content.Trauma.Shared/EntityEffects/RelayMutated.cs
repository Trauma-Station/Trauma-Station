// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// For a mutation target, relays an effect to the target mob.
/// </summary>
public sealed partial class RelayMutated : EntityEffectBase<RelayMutated>
{
    /// <summary>
    /// Effect to apply to the implanted entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-relay-mutated", ("chance", Probability), ("effect", Effect.EntityEffectGuidebookText(prototype, entSys) ?? string.Empty));
}

public sealed class RelayMutatedEffectSystem : EntityEffectSystem<MutationComponent, RelayMutated>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<MutationComponent> ent, ref EntityEffectEvent<RelayMutated> args)
    {
        if (ent.Comp.Target is {} mob)
            _effects.TryApplyEffect(mob, args.Effect.Effect, args.Scale);
    }
}
