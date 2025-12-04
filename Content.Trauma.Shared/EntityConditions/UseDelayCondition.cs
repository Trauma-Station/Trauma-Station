// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that requires a use delay to not be active.
/// </summary>
public sealed partial class UseDelayCondition : EntityConditionBase<UseDelayCondition>
{
    /// <summary>
    /// The specific use delay to check.
    /// </summary>
    [DataField]
    public string DelayId = UseDelaySystem.DefaultId;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-use-delay", ("id", DelayId));
}

public sealed class UseDelayConditionSystem : EntityConditionSystem<UseDelayComponent, UseDelayCondition>
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    protected override void Condition(Entity<UseDelayComponent> ent, ref EntityConditionEvent<UseDelayCondition> args)
    {
        var id = args.Condition.DelayId;
        args.Result = !_useDelay.IsDelayed((ent, ent.Comp), id);
    }
}
