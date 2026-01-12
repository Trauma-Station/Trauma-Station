// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks that the target entity has a certain marking present.
/// </summary>
public sealed partial class HasMarking : EntityConditionBase<HasMarking>
{
    /// <summary>
    /// The category the marking must be in.
    /// </summary>
    [DataField(required: true)]
    public MarkingCategories Category;

    /// <summary>
    /// The category the marking must be in.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    /// <summary>
    /// Guidebook text explaining the condition.
    /// </summary>
    [DataField]
    public LocId GuidebookText = "entity-condition-guidebook-has-marking";

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString(GuidebookText, ("marking", prototype.Index(Marking).Name));
}

public sealed class HasMarkingConditionSystem : EntityConditionSystem<HumanoidAppearanceComponent, HasMarking>
{
    protected override void Condition(Entity<HumanoidAppearanceComponent> ent, ref EntityConditionEvent<HasMarking> args)
    {
        args.Result = HasMarking(ent.Comp.MarkingSet, args.Condition);
    }

    public bool HasMarking(MarkingSet markings, HasMarking cond)
        => markings.TryGetMarking(cond.Category, cond.Marking, out _);
}
