// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a child part slot from the target entity, which must be a body part.
/// </summary>
public sealed partial class RemovePartSlot : EntityEffectBase<RemovePartSlot>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-part-remove-slot", ("chance", Probability), ("slot", Slot));
}

public sealed class RemovePartSlotEffectSystem : EntityEffectSystem<BodyPartComponent, RemovePartSlot>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<RemovePartSlot> args)
    {
        _body.RemovePartSlot(ent, args.Effect.Slot);
    }
}
