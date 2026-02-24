// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

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

public sealed class InsertNewOrganEffectSystem : EntityEffectSystem<BodyPartComponent, InsertNewOrgan>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<InsertNewOrgan> args)
    {
        var organ = PredictedSpawnAtPosition(args.Effect.Organ, Transform(ent).Coordinates);
        if (_body.GetCategory(organ) is not {} category)
        {
            Log.Error($"Tried to insert invalid organ {ToPrettyString(organ)} into {ToPrettyString(ent)}!");
            PredictedDel(organ);
            return;
        }

        // this specifically is a programmer error
        if (!ent.Comp.Slots.Contains(category))
        {
            Log.Error($"Tried to insert organ {ToPrettyString(organ)} into {ToPrettyString(ent)} which has no {category} slot!");
            PredictedDel(organ);
            return;
        }

        if (!_part.InsertOrgan(ent.AsNullable(), organ))
        {
            Log.Warning($"Failed to insert organ {ToPrettyString(organ)} into {ToPrettyString(ent)}'s {category} slot.");
            PredictedDel(organ);
        }
    }
}
