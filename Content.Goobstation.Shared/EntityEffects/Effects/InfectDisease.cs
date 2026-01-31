using Content.Goobstation.Shared.Disease.Components;
using Content.Goobstation.Shared.Disease.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Infects the target mob with a disease.
/// </summary>
public sealed partial class InfectDisease : EntityEffectBase<InfectDisease>
{
    [DataField(required: true)]
    public EntProtoId Disease;

    [DataField]
    public bool Mutate;

    [DataField]
    public float? MutationRate;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-infect-disease", ("chance", Probability), ("disease", prototype.Index(Disease).Name));
}

public sealed class InfectDiseaseEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, InfectDisease>
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> ent, ref EntityEffectEvent<InfectDisease> args)
    {
        var effect = args.Effect;
        if (_disease.TryInfect(ent.AsNullable(), effect.Disease, out var disease) && effect.Mutate)
            _disease.MutateDisease(disease.Value, effect.MutationRate);
    }
}
