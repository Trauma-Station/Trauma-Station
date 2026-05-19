// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Chaplain.Components;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that checks if you have a certain amount of nullification.
/// </summary>
public sealed partial class HasNullificationCondition : EntityConditionBase<HasNullificationCondition>
{
    /// <summary>
    /// The nullification to check for.
    /// </summary>
    [DataField(required: true)]
    public int Amount;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // TODO: add
}

public sealed class HasNullificationConditionSystem : EntityConditionSystem<NullificationComponent, HasNullificationCondition>
{
    protected override void Condition(Entity<NullificationComponent> ent, ref EntityConditionEvent<HasNullificationCondition> args)
    {
        args.Result = ent.Comp.CurrentNullification >= args.Condition.Amount;
    }
}
