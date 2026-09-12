// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that requires the target entity is moving.
/// </summary>
public sealed partial class MovingCondition : EntityConditionBase<MovingCondition>
{
    [DataField]
    public float MinVelocity = 1f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-moving", ("speed", MinVelocity));
}

public sealed partial class MovingConditionSystem : EntityConditionSystem<PhysicsComponent, MovingCondition>
{
    protected override void Condition(Entity<PhysicsComponent> ent, ref EntityConditionEvent<MovingCondition> args)
    {
        var min = args.Condition.MinVelocity;
        args.Result = ent.Comp.LinearVelocity.LengthSquared() >= min * min;
    }
}
