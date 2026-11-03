using Content.Shared.Actions.Components;
using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that checks if you have a specified action prototype
/// </summary>
public sealed partial class HasActionCondition : EntityConditionBase<HasActionCondition>
{
    [DataField(required: true)]
    public EntProtoId Action;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class HasActionConditionSystem : EntityConditionSystem<ActionsContainerComponent, HasActionCondition>
{
    protected override void Condition(Entity<ActionsContainerComponent> ent, ref EntityConditionEvent<HasActionCondition> args)
    {
        var result = args.Condition.Action;

        var found = false;
        foreach (var action in ent.Comp.Container.ContainedEntities)
        {
            if (Prototype(action) is not { } actionProto)
                continue;

            if (actionProto == result)
                found = true;
        }

        args.Result = found;
    }
}
