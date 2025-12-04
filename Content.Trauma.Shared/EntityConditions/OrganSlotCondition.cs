// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target mob has an organ slot in a body part.
/// </summary>
public sealed partial class OrganSlotCondition : EntityConditionBase<OrganSlotCondition>
{
    /// <summary>
    /// Organ slot ID that must exist in a found body part.
    /// </summary>
    [DataField(required: true)]
    public string Organ = string.Empty;

    [DataField(required: true)]
    public BodyPartType PartType;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-organ-slot", ("inverted", Inverted), ("part", PartType), ("slot", Organ));
}

public sealed class OrganSlotConditionSystem : EntityConditionSystem<BodyComponent, OrganSlotCondition>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> ent, ref EntityConditionEvent<OrganSlotCondition> args)
    {
        var slot = args.Condition.Organ;
        var partType = args.Condition.PartType;
        var symmetry = args.Condition.Symmetry;
        foreach (var (_, part) in _body.GetBodyChildrenOfType(ent, partType, ent.Comp, symmetry))
        {
            if (part.Organs.ContainsKey(slot))
            {
                args.Result = true;
                return;
            }
        }
    }
}
