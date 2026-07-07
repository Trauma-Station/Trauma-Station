// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Mind.Components;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks that the target mob had mind at some point
/// </summary>
public sealed partial class HadMindCondition : EntityConditionBase<HadMindCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class HadMindConditionSystem : EntityConditionSystem<MindContainerComponent, HadMindCondition>
{
    protected override void Condition(Entity<MindContainerComponent> ent, ref EntityConditionEvent<HadMindCondition> args)
    {
        args.Result = ent.Comp.HadMind;
    }
}
