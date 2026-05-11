// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that checks whether the target is standing on a grid, or not.
/// </summary>
public sealed partial class OnGridCondition : EntityConditionBase<OnGridCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed class OnGridConditionSystem : EntityConditionSystem<TransformComponent, OnGridCondition>
{
    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<OnGridCondition> args)
    {
        args.Result = ent.Comp.GridUid != null;
    }
}
