// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Deletes the target entity, be careful with this...
/// </summary>
public sealed partial class Delete : EntityEffectBase<Delete>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-delete-entity", ("chance", Probability));
}

public sealed class DeleteEffectSystem : EntityEffectSystem<MetaDataComponent, Delete>
{
    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<Delete> args)
    {
        PredictedDel(ent.AsNullable());
    }
}
