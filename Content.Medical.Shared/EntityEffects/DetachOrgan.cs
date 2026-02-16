// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Detaches this target organ/part from its parent part.
/// </summary>
public sealed partial class DetachOrgan : EntityEffectBase<DetachOrgan>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-detach-part", ("chance", Probability));
}

public sealed class DetachOrganEffectSystem : EntityEffectSystem<ChildOrganComponent, DetachOrgan>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<ChildOrganComponent> ent, ref EntityEffectEvent<DetachOrgan> args)
    {
        if (ent.Comp.Parent is not {} parent)
            return;

        _part.RemoveOrgan(parent, ent.Owner);
    }
}
