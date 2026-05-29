// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Spawns and inserts an organ/bodypart into the target entity, which must be a bodypart.
/// The slot must exist and not be occupied.
/// </summary>
public sealed partial class InsertNewOrgan : EntityEffectBase<InsertNewOrgan>
{
    /// <summary>
    /// The organ/bodypart to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<OrganComponent> Organ;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-insert-new-organ", ("chance", Probability), ("organ", prototype.Index(Organ).Name));
}

public sealed partial class InsertNewOrganEffectSystem : EntityEffectSystem<BodyPartComponent, InsertNewOrgan>
{
    [Dependency] private BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<InsertNewOrgan> args)
    {
        _part.SpawnAndInsert(ent.AsNullable(), args.Effect.Organ);
    }
}
