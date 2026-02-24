// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Removes an organ slot from the target entity, which must be a body part.
/// </summary>
public sealed partial class RemoveOrganSlot : EntityEffectBase<RemoveOrganSlot>
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Slot;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-part-remove-slot", ("chance", Probability), ("slot", Slot));
}

public sealed class RemoveOrganSlotEffectSystem : EntityEffectSystem<BodyPartComponent, RemoveOrganSlot>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<RemoveOrganSlot> args)
    {
        _part.TryRemoveSlot(ent.AsNullable(), args.Effect.Slot);
    }
}
