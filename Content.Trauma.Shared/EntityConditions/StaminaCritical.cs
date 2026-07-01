// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that is true if the target entity is stamcrit.
/// </summary>
public sealed partial class StaminaCritical : EntityConditionBase<StaminaCritical>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed class StaminaCriticalConditionSystem : EntityConditionSystem<StaminaComponent, StaminaCritical>
{
    protected override void Condition(Entity<StaminaComponent> ent, ref EntityConditionEvent<StaminaCritical> args)
    {
        args.Result = ent.Comp.Critical;
    }
}
