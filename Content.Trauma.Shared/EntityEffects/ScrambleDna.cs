// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Scrambles the target entity's genome.
/// </summary>
public sealed partial class ScrambleDna : EntityEffectBase<ScrambleDna>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-scramble-dna", ("chance", Probability));
}

public sealed class ScrambleDnaEffectSystem : EntityEffectSystem<MutatableComponent, ScrambleDna>
{
    [Dependency] private readonly MutationSystem _mutation = default!;

    protected override void Effect(Entity<MutatableComponent> ent, ref EntityEffectEvent<ScrambleDna> args)
    {
        _mutation.Scramble(ent);
    }
}
