// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.EntityConditions;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks that the target mob has a certain marking present on a part.
/// </summary>
public sealed partial class HasMarking : EntityConditionBase<HasMarking>
{
    /// <summary>
    /// The organ the marking must be in.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    /// <summary>
    /// The marking to look for.
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

public sealed class HasMarkingConditionSystem : EntityConditionSystem<BodyComponent, HasMarking>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> ent, ref EntityConditionEvent<HasMarking> args)
    {
        args.Result = HasMarking(ent.AsNullable(), args.Condition);
    }

    // closest thing to an api body visuals has lol
    public bool HasMarking(Entity<BodyComponent?> body, HasMarking cond)
    {
        if (_body.GetOrgan(body, cond.Organ) is not {} organ ||
            !TryComp<VisualOrganMarkingsComponent>(organ, out var comp))
            return false; // no part lol

        var marking = _proto.Index(cond.Marking);
        var markings = comp.Markings;
        if (!markings.TryGetValue(marking.BodyPart, out var list))
            return false; // layer not present, marking can't be

        // look for the marking
        foreach (var data in list)
        {
            if (data.MarkingId == marking.ID)
                return true;
        }

        return false;
    }
}
