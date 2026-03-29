// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Rituals.EntityEffects;

public sealed class OnFireConditionSystem : EntityConditionSystem<FlammableComponent, OnFireCondition>
{
    protected override void Condition(Entity<FlammableComponent> entity, ref EntityConditionEvent<OnFireCondition> args)
    {
        args.Result = entity.Comp.OnFire;
    }
}

public sealed partial class OnFireCondition : EntityConditionBase<OnFireCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-on-fire", ("invert", Inverted));
    }
}
