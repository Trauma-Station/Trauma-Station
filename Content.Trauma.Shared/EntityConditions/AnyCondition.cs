// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Localizations;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the target entity against multiple conditions, passing if at least 1 does.
/// </summary>
public sealed partial class AnyCondition : EntityConditionBase<AnyCondition>
{
    [DataField(required: true)]
    public EntityCondition[] Conditions = default!;

    private List<string> _conditions = new();

    public override string EntityConditionGuidebookText(IPrototypeManager proto)
    {
        _conditions.Clear();
        foreach (var condition in Conditions)
        {
            _conditions.Add(condition.EntityConditionGuidebookText(proto));
        }
        return ContentLocalizationManager.FormatListToOr(_conditions);
    }
}

public sealed partial class AnyConditionSystem : EntityConditionSystem<MetaDataComponent, AnyCondition>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<AnyCondition> args)
    {
        args.Result = _conditions.TryAnyCondition(ent, args.Condition.Conditions);
    }
}
