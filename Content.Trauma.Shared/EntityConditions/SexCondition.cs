// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Humanoid;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// 18+ condition sorry chuds
/// </summary>
public sealed partial class SexCondition : EntityConditionBase<SexCondition>
{
    [DataField(required: true)]
    public Sex Sex;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class SexConditionSystem : EntityConditionSystem<HumanoidProfileComponent, SexCondition>
{
    protected override void Condition(Entity<HumanoidProfileComponent> ent, ref EntityConditionEvent<SexCondition> args)
    {
        args.Result = ent.Comp.Sex == args.Condition.Sex;
    }
}
