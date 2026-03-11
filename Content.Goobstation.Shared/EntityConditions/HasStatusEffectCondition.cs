// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityConditions;

public sealed partial class HasStatusEffectCondition : EntityConditionBase<HasStatusEffectCondition>
{
    [DataField(required: true)]
    public EntProtoId EffectProto;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("reagent-effect-guidebook-has-status-effect",
            ("effect", prototype.Index(EffectProto).Name),
            ("invert", Inverted));
}

public sealed class HasStatusEffectConditionSystem : EntityConditionSystem<MetaDataComponent, HasStatusEffectCondition>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    protected override void Condition(Entity<MetaDataComponent> entity,
        ref EntityConditionEvent<HasStatusEffectCondition> args)
    {
        args.Result = _status.HasStatusEffect(entity.Owner, args.Condition.EffectProto);
    }
}
