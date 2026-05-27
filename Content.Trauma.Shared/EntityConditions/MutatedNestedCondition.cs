// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the mutated mob against a nested condition.
/// If this condition's target is not a mutation entity it always returns false.
/// </summary>
public sealed partial class MutatedNestedCondition : EntityConditionBase<MutatedNestedCondition>
{
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Condition.EntityConditionGuidebookText(prototype);
}

public sealed partial class MutatedNestedConditionSystem : EntityConditionSystem<MutationComponent, MutatedNestedCondition>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected override void Condition(Entity<MutationComponent> ent, ref EntityConditionEvent<MutatedNestedCondition> args)
    {
        if (ent.Comp.Target is {} target)
            args.Result = _conditions.TryCondition(target, args.Condition.Condition);
    }
}
