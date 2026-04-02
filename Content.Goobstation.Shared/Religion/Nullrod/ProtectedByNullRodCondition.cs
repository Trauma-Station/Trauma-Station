// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Religion.Nullrod;

public sealed partial class
    ProtectedByNullRodEntityConditionSystem : EntityConditionSystem<MetaDataComponent, ProtectedByNullRodCondition>
{
    [Dependency] private readonly DivineInterventionSystem _divine = default!;

    protected override void Condition(Entity<MetaDataComponent> entity,
        ref EntityConditionEvent<ProtectedByNullRodCondition> args)
    {
        args.Result = _divine.TouchSpellDenied(entity, false);
    }
}

public sealed partial class ProtectedByNullRodCondition : EntityConditionBase<ProtectedByNullRodCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Inverted
            ? Loc.GetString("entity-condition-guidebook-nullrod-not-protected")
            : Loc.GetString("entity-condition-guidebook-nullrod-protected");
    }
}
