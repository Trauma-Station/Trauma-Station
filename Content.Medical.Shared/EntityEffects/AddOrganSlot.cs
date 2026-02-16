// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Adds a organ/bodypart slot to the target entity, which must be a body part.
/// </summary>
public sealed partial class AddOrganSlot : EntityEffectBase<AddOrganSlot>
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-part-add-slot", ("chance", Probability), ("slot", prototype.Index(Category).Name));
}

public sealed class AddOrganSlotEffectSystem : EntityEffectSystem<BodyPartComponent, AddOrganSlot>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<AddOrganSlot> args)
    {
        _part.TryAddSlot(ent.AsNullable(), args.Effect.Category);
    }
}
