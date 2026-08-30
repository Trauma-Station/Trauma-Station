// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Roles;
using Content.Trauma.Shared.Areas;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks that the target entity is in an area belonging to a department.
/// </summary>
public sealed partial class InDepartmentArea : EntityConditionBase<InDepartmentArea>
{
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // set it if you add it to a reagent...
}

public sealed partial class InDepartmentAreaConditionSystem : EntityConditionSystem<TransformComponent, InDepartmentArea>
{
    [Dependency] private AreaSystem _area = default!;

    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<InDepartmentArea> args)
    {
        args.Result = _area.GetArea(ent.Comp.Coordinates) is { } area &&
            _area.GetAreaDepartment(area) is { } dep &&
            dep == args.Condition.Department;
    }

}
