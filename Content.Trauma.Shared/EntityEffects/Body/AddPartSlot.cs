// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a child part slot to the target entity, which must be a body part.
/// </summary>
public sealed partial class AddPartSlot : EntityEffectBase<AddPartSlot>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField(required: true)]
    public BodyPartType PartType;

    [DataField(required: true)]
    public BodyPartSymmetry Symmetry;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-part-add-slot", ("chance", Probability), ("slot", Slot));
}

public sealed class AddPartSlotEffectSystem : EntityEffectSystem<BodyPartComponent, AddPartSlot>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<AddPartSlot> args)
    {
        var effect = args.Effect;
        _body.CreatePartSlot(ent, effect.Slot, effect.PartType, effect.Symmetry, ent.Comp);
    }
}
