// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.EntityConditions;

namespace Content.Medical.Shared.EntityConditions;

/// <summary>
/// Condition that checks if the target mob has at least 1 organ of a given category.
/// Since it uses organ categories, this is symmetry sensitive.
/// Optionally can check nested entity conditions against the organ.
/// </summary>
public sealed partial class HasOrgan : EntityConditionBase<HasOrgan>
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganCategory;

    /// <summary>
    /// Optional conditions the organ must meet.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-has-organ",
            ("invert", Inverted),
            ("organ", OrganCategory));
}

public sealed partial class HasOrganConditionSystem : EntityConditionSystem<BodyComponent, HasOrgan>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected override void Condition(Entity<BodyComponent> entity, ref EntityConditionEvent<HasOrgan> args)
    {
        args.Result = _body.GetOrgan(entity.AsNullable(), args.Condition.OrganCategory) is {} organ &&
            _conditions.TryConditions(organ, args.Condition.Conditions);
    }
}
