// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies effects to a solution entity of a given name.
/// </summary>
public sealed partial class RelaySolution : EntityEffectBase<RelaySolution>
{
    /// <summary>
    /// The solution to get.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Effects to apply to the solution entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // cbf
}

public sealed class RelaySolutionEffectSystem : EntityEffectSystem<SolutionContainerManagerComponent, RelaySolution>
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    protected override void Effect(Entity<SolutionContainerManagerComponent> ent, ref EntityEffectEvent<RelaySolution> args)
    {
        if (!_solution.TryGetSolution(ent.AsNullable(), args.Effect.Name, out var solution, out _, true))
            return;

        var uid = solution.Value.Owner;
        _data.CopyData(ent, uid);
        _effects.ApplyEffects(uid, args.Effect.Effects, args.Scale, args.User);
        _data.ClearData(uid);
    }
}
