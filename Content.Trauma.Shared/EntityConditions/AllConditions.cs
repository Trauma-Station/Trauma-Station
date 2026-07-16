// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Localizations;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the target entity against multiple conditions, passing if all do.
/// </summary>
public sealed partial class AllConditions : EntityConditionBase<AllConditions>
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
        return ContentLocalizationManager.FormatList(_conditions);
    }
}

public sealed partial class AllConditionsSystem : EntityConditionSystem<MetaDataComponent, AllConditions>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<AllConditions> args)
    {
        args.Result = _conditions.TryConditions(ent, args.Condition.Conditions, args.User);
    }
}
