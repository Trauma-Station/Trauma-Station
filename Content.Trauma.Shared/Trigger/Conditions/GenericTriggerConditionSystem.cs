// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Trigger;

namespace Content.Trauma.Shared.Trigger.Conditions;

public sealed class GenericTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<GenericTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key is not {} key || !ent.Comp.Keys.Contains(key))
            return;

        if ((ent.Comp.CheckUser ? args.User : ent.Owner) is not {} target)
        {
            args.Cancelled = true;
            return;
        }

        args.Cancelled |= !_conditions.TryCondition(target, ent.Comp.Condition);
    }
}
