// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Trauma.Common.Reagents;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that adjusts any reagent in the target's specified solution that belongs to a specified reagent group.
/// The reagent must exist in the target's solution.
/// </summary>
public sealed partial class AdjustReagentGroup : EntityEffectBase<AdjustReagentGroup>
{
    /// <summary>
    /// How much to adjust the reagent
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Amount;

    /// <summary>
    /// Which reagent group to adjust.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentGroupPrototype> Group;
}

public sealed partial class AdjustReagentGroupEffectSystem : EntityEffectSystem<SolutionComponent, AdjustReagentGroup>
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    protected override void Effect(Entity<SolutionComponent> ent, ref EntityEffectEvent<AdjustReagentGroup> args)
    {
        var quantity = args.Effect.Amount * args.Scale;
        var reagentGroup = args.Effect.Group;

        foreach (var (reagent, _) in ent.Comp.Solution.GetReagentPrototypes(_proto))
        {
            if (reagent.Group != reagentGroup)
                continue;

            if (quantity > 0)
                _solution.TryAddReagent(ent, reagent.ID, quantity);
            else
                _solution.RemoveReagent(ent, reagent.ID, -quantity);
        }
    }
}
