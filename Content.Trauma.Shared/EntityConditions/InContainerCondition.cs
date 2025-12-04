// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is inside of any container.
/// </summary>
public sealed partial class InContainerCondition : EntityConditionBase<InContainerCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-standing");
}

public sealed partial class InContainerConditionSystem : EntityConditionSystem<MetaDataComponent, InContainerCondition>
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<InContainerCondition> args)
    {
        args.Result = _container.TryGetContainingContainer((ent, null, ent.Comp), out _);
    }
}
