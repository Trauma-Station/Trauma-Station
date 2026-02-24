// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityConditions;

/// <summary>
/// Requires that the target mob has an organ slot in a body part.
/// </summary>
public sealed partial class HasOrganSlot : EntityConditionBase<HasOrganSlot>
{
    /// <summary>
    /// Organ slot ID that must exist in a found body part.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField(required: true)]
    public BodyPartType PartType;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-organ-slot", ("inverted", Inverted), ("part", PartType), ("slot", Organ));
}

public sealed class HasOrganSlotConditionSystem : EntityConditionSystem<BodyComponent, HasOrganSlot>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Condition(Entity<BodyComponent> ent, ref EntityConditionEvent<HasOrganSlot> args)
    {
        var slot = args.Condition.Organ;
        var partType = args.Condition.PartType;
        var symmetry = args.Condition.Symmetry;
        foreach (var part in _part.GetBodyParts(ent, partType, symmetry))
        {
            if (_part.HasOrganSlot(part, slot))
            {
                args.Result = true;
                return;
            }
        }
    }
}
