// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is anchored.
/// </summary>
public sealed partial class AnchoredCondition : EntityConditionBase<AnchoredCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager proto)
        => string.Empty;
}

public sealed partial class AnchoredConditionSystem : EntityConditionSystem<TransformComponent, AnchoredCondition>
{
    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<AnchoredCondition> args)
    {
        args.Result = ent.Comp.Anchored;
    }
}
