// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is not anchored
/// </summary>
public sealed partial class UnanchoredCondition : EntityConditionBase<UnanchoredCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager proto)
        => string.Empty;
}

public sealed partial class UnanchoredConditionSystem : EntityConditionSystem<TransformComponent, UnanchoredCondition>
{
    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<UnanchoredCondition> args)
    {
        args.Result = !ent.Comp.Anchored;
    }
}
