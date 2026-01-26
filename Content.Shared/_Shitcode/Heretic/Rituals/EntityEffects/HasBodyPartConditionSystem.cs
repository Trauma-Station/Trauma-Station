using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Rituals.EntityEffects;

public sealed class HasBodyPartConditionSystem : EntityConditionSystem<BodyComponent, HasBodyPartCondition>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> entity, ref EntityConditionEvent<HasBodyPartCondition> args)
    {
        args.Result = _body.GetBodyChildrenOfType(entity, args.Condition.Part).Any();
    }
}

public sealed partial class HasBodyPartCondition : EntityConditionBase<HasBodyPartCondition>
{
    [DataField]
    public BodyPartType Part;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-has-body-part",
            ("invert", Inverted),
            ("part", Part.ToString()));
    }
}
