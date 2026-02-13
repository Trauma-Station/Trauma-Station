// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is a humanoid of a given species.
/// </summary>
public sealed partial class SpeciesCondition : EntityConditionBase<SpeciesCondition>
{
    /// <summary>
    /// The species to check.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-is-species", ("species", prototype.Index(Species).Name));
}

public sealed partial class SpeciesConditionSystem : EntityConditionSystem<HumanoidProfileComponent, SpeciesCondition>
{
    protected override void Condition(Entity<HumanoidProfileComponent> ent, ref EntityConditionEvent<SpeciesCondition> args)
    {
        args.Result = ent.Comp.Species == args.Condition.Species;
    }
}
