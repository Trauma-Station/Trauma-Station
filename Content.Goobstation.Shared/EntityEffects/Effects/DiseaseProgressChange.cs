using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Goobstation.Shared.Disease.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Reduces the progress of diseases of chosen type on the entity.
/// </summary>
public sealed partial class DiseaseProgressChange : EntityEffectBase<DiseaseProgressChange>
{
    /// <summary>
    /// Diseases of which type to affect.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DiseaseTypePrototype> AffectedType;

    /// <summary>
    /// How much to add to the disease progress.
    /// </summary>
    [DataField]
    public float ProgressModifier = -0.02f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-disease-progress-change",
            ("chance", Probability),
            ("type", prototype.Index<DiseaseTypePrototype>(AffectedType).LocalizedName),
            ("amount", ProgressModifier));
}

public sealed class DiseaseProgressChangeEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, DiseaseProgressChange>
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> ent, ref EntityEffectEvent<DiseaseProgressChange> args)
    {
        var amt = args.Effect.ProgressModifier * args.Scale;

        var affected = args.Effect.AffectedType;
        foreach (var diseaseUid in ent.Comp.Diseases)
        {
            if (!TryComp<DiseaseComponent>(diseaseUid, out var disease) || disease.DiseaseType != affected)
                continue;

            _disease.ChangeInfectionProgress((diseaseUid, disease), amt);
        }
    }
}
