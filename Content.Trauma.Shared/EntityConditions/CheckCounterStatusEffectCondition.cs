// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.StatusEffects;

namespace Content.Trauma.Shared.EntityConditions;

public sealed partial class CheckCounterStatusEffectCondition : EntityConditionBase<CheckCounterStatusEffectCondition>
{
    [DataField(required: true)]
    public EntProtoId<CounterStatusEffectComponent> Status;

    /// <summary>
    /// If counter is less than this - return true
    /// Else return false
    /// </summary>
    [DataField(required: true)]
    public int Max;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class
    CheckCounterStatusEffectConditionSystem : EntityConditionSystem<TransformComponent,
    CheckCounterStatusEffectCondition>
{
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void Condition(Entity<TransformComponent> ent,
        ref EntityConditionEvent<CheckCounterStatusEffectCondition> args)
    {
        args.Result = !_status.TryGetStatusEffect(ent, args.Condition.Status, out var effect) ||
                      Comp<CounterStatusEffectComponent>(effect.Value).Count < args.Condition.Max;
    }
}
