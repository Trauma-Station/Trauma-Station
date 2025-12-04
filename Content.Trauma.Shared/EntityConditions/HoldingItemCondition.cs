// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is holding an item.
/// </summary>
public sealed partial class HoldingItemCondition : EntityConditionBase<HoldingItemCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-holding-item");
}

public sealed partial class HoldingItemConditionSystem : EntityConditionSystem<HandsComponent, HoldingItemCondition>
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    protected override void Condition(Entity<HandsComponent> ent, ref EntityConditionEvent<HoldingItemCondition> args)
    {
        foreach (var item in _hands.EnumerateHeld((ent, ent.Comp)))
        {
            args.Result = true;
            return;
        }
    }
}
