// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Detaches this target part from its body.
/// </summary>
public sealed partial class DetachPart : EntityEffectBase<DetachPart>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-detach-part", ("chance", Probability));
}

public sealed class DetachPartEffectSystem : EntityEffectSystem<BodyPartComponent, DetachPart>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<DetachPart> args)
    {
        if (_body.GetParentPartOrNull(ent) is not {} parent)
            return;

        _body.DropSlotContents(ent); // TODO: check if theres other parts present, rn this is only used for head which is fine
        var slot = _body.GetSlotFromBodyPart(ent.Comp);
        _body.DetachPart(parent, slot, ent, part: ent.Comp);
    }
}
