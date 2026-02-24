// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity has a string in its name.
/// </summary>
public sealed partial class NameContainsCondition : EntityConditionBase<NameContainsCondition>
{
    /// <summary>
    /// The string that needs to be contained in the entity name.
    /// This is checked insensitive of case.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-has-mob-name", ("name", Name));
}

public sealed partial class NameContainsConditionSystem : EntityConditionSystem<MetaDataComponent, NameContainsCondition>
{
    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<NameContainsCondition> args)
    {
        var entName = ent.Comp.EntityName;
        args.Result = entName.IndexOf(args.Condition.Name, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
