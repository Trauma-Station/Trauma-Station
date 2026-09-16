// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Hands;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity has at least 1 empty hand.
/// </summary>
public sealed partial class EmptyHandCondition : EntityConditionBase<EmptyHandCondition>;

public sealed partial class EmptyHandConditionSystem : EntityConditionSystem<HandsComponent, EmptyHandCondition>
{
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Condition(Entity<HandsComponent> ent, ref EntityConditionEvent<EmptyHandCondition> args)
    {
        args.Result = _hands.TryGetEmptyHand(ent.AsNullable(), out _);
    }
}
