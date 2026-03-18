// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity has a vital damage within some bounds.
/// </summary>
public sealed partial class VitalDamageCondition : EntityConditionBase<VitalDamageCondition>
{
    [DataField]
    public FixedPoint2 Min;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-vital-damage",
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
}

public sealed partial class VitalDamageConditionSystem : EntityConditionSystem<DamageableComponent, VitalDamageCondition>
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;

    protected override void Condition(Entity<DamageableComponent> ent, ref EntityConditionEvent<VitalDamageCondition> args)
    {
        var vital = _threshold.CheckVitalDamage(ent.AsNullable());
        args.Result = vital >= args.Condition.Min && vital <= args.Condition.Max;
    }
}
