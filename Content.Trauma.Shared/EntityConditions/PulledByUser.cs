// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that requires the target entity is being pulled by the user.
/// Always false if a user was not passed.
/// </summary>
public sealed partial class PulledByUser : EntityConditionBase<PulledByUser>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class PulledByUserConditionSystem : EntityConditionSystem<PullableComponent, PulledByUser>
{
    protected override void Condition(Entity<PullableComponent> ent, ref EntityConditionEvent<PulledByUser> args)
    {
        args.Result = args.User is { } puller && ent.Comp.Puller == puller;
    }
}
