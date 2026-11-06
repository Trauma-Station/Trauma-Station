// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Movement.Systems;

namespace Content.Trauma.Shared.Movement;

public sealed partial class ConditionMovementSpeedSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;

    [SubscribeLocalEvent]
    private void OnRefreshMovement(Entity<ConditionMovementSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        foreach (var (speed, cond) in ent.Comp.Conditions)
        {
            var passed = ent.Comp.Any
                ? _condition.TryAnyCondition(ent, cond)
                : _condition.TryConditions(ent, cond);
            if (!passed)
                continue;

            args.ModifySpeed(speed.X, speed.Y);
        }
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<ConditionMovementSpeedComponent> ent, ref MoveEvent args)
    {
        // idfk
        if (TerminatingOrDeleted(ent))
            return;

        _movement.RefreshMovementModifiers(ent.Owner);
    }
}
