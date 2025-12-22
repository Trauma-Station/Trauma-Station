using Content.Goobstation.Shared.Disease.Components;
using Content.Goobstation.Shared.Disease.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Mutates diseases on the entity.
/// </summary>
public sealed partial class MutateDiseases : EntityEffectBase<MutateDiseases>
{
    /// <summary>
    /// How much to mutate.
    /// </summary>
    [DataField]
    public float MutationRate = 0.05f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-disease-mutate",
            ("amount", MutationRate));
}

public sealed class MutateDiseasesEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, MutateDiseases>
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> ent, ref EntityEffectEvent<MutateDiseases> args)
    {
        foreach (var disease in ent.Comp.Diseases)
        {
            var amt = args.Effect.MutationRate * args.Scale;
            _disease.MutateDisease(disease, amt);
        }
    }
}
