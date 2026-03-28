// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.EntityConditions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks that the target mob has any marking of a given layer present on a part.
/// </summary>
public sealed partial class HasAnyMarking : EntityConditionBase<HasAnyMarking>
{
    /// <summary>
    /// The organ the marking must be in.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    /// <summary>
    /// The layer a marking must match.
    /// </summary>
    [DataField(required: true)]
    public HumanoidVisualLayers Layer;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed class HasAnyMarkingConditionSystem : EntityConditionSystem<BodyComponent, HasAnyMarking>
{
    [Dependency] private readonly BodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> ent, ref EntityConditionEvent<HasAnyMarking> args)
    {
        args.Result = HasAnyMarking(ent.AsNullable(), args.Condition);
    }

    // closest thing to an api body visuals has lol
    public bool HasAnyMarking(Entity<BodyComponent?> body, HasAnyMarking cond)
    {
        if (_body.GetOrgan(body, cond.Organ) is not {} organ ||
            !TryComp<VisualOrganMarkingsComponent>(organ, out var comp))
            return false; // no part lol

        var markings = comp.Markings;
        if (!markings.TryGetValue(cond.Layer, out var list))
            return false; // layer not present, marking can't be

        // if the list isn't empty there should be a marking with the right layer
        return list.Count > 0;
    }
}
