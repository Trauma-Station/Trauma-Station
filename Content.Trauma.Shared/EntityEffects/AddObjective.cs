// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds an objective to the target mob, which needs a mind container.
/// </summary>
public sealed partial class AddObjective : EntityEffectBase<AddObjective>
{
    /// <summary>
    /// The objective to add.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ObjectiveComponent> Objective;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // make it if you put this on a reagent (and die for doing that)
}

public sealed class AddObjectiveEffectSystem : EntityEffectSystem<MindContainerComponent, AddObjective>
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    protected override void Effect(Entity<MindContainerComponent> ent, ref EntityEffectEvent<AddObjective> args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mind, ent.Comp))
            return;

        _mind.TryAddObjective(mindId, mind, args.Effect.Objective);
    }
}
