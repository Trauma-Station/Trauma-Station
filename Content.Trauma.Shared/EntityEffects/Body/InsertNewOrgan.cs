using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns and inserts an organ into the target entity, which must be a body part.
/// </summary>
public sealed partial class InsertNewOrgan : EntityEffectBase<InsertNewOrgan>
{
    /// <summary>
    /// The organ to spawn.
    /// The slot used is from <c>OrganComponent.SlotId</c>.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<OrganComponent> Organ;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-insert-new-organ", ("chance", Probability), ("organ", prototype.Index(Organ).Name));
}

public sealed class InsertNewOrganEffectSystem : EntityEffectSystem<BodyPartComponent, InsertNewOrgan>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    private EntityQuery<OrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        _organQuery = GetEntityQuery<OrganComponent>();
    }

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<InsertNewOrgan> args)
    {
        if (ent.Comp.Body is not {} body)
            return;

        var organ = PredictedSpawnAtPosition(args.Effect.Organ, Transform(body).Coordinates);
        var organComp = _organQuery.Comp(organ);
        if (organComp.SlotId == string.Empty)
        {
            // programmer error
            Log.Error($"Organ {ToPrettyString(organ)} has no slot defined!");
            PredictedDel(organ);
            return;
        }

        if (!_body.InsertOrgan(ent, organ, organComp.SlotId, ent.Comp, organComp))
        {
            // could be a bad target used, not necessarily error
            Log.Warning($"Could not insert organ {ToPrettyString(organ)} into {ToPrettyString(ent)}'s {organComp.SlotId} slot!");
            PredictedDel(organ);
        }
    }
}
