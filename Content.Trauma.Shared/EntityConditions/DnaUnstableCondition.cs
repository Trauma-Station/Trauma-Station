// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target mob has 100 or more DNA instability.
/// </summary>
public sealed partial class DnaUnstableCondition : EntityConditionBase<DnaUnstableCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-dna-unstable");
}

public sealed class DnaUnstableConditionSystem : EntityConditionSystem<MutatableComponent, DnaUnstableCondition>
{
    protected override void Condition(Entity<MutatableComponent> ent, ref EntityConditionEvent<DnaUnstableCondition> args)
    {
        args.Result = ent.Comp.TotalInstability >= ent.Comp.MaxInstability;
    }
}
