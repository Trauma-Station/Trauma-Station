using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Goobstation.Shared.Disease.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Disease;

public sealed partial class DiseaseSystem : SharedDiseaseSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantDiseaseComponent, MapInitEvent>(OnGrantDiseaseInit);
        // SubscribeLocalEvent<InternalsComponent, DiseaseIncomingSpreadAttemptEvent>(OnInternalsIncomingSpread); // TODO: fix
    }

    private void OnGrantDiseaseInit(Entity<GrantDiseaseComponent> ent, ref MapInitEvent args)
    {
        if (MakeRandomDisease(ent.Comp.BaseDisease, ent.Comp.Complexity) is not {} disease)
            return;

        if (TryComp<DiseaseComponent>(disease, out var diseaseComp))
        {
            if (ent.Comp.PossibleTypes != null)
                diseaseComp.DiseaseType = _random.Pick(ent.Comp.PossibleTypes);

            diseaseComp.InfectionProgress = ent.Comp.Severity;
        }

        if (!TryInfect(ent.Owner, disease))
            Del(disease);
    }

    /* TODO: fix
    private void OnInternalsIncomingSpread(EntityUid uid, InternalsComponent internals, DiseaseIncomingSpreadAttemptEvent args)
    {
        if (_proto.TryIndex(args.Type, out var spreadProto) && _internals.AreInternalsWorking(uid, internals))
        {
            args.ApplyModifier(internals.IncomingInfectionModifier);
        }
    }
    */

    #region public API

    /// <summary>
    /// Makes a random disease from a base prototype
    /// By default, will avoid changing anything already present in the base prototype
    /// </summary>
    public override EntityUid? MakeRandomDisease(EntProtoId baseProto, float complexity, float mutationRate = 0f)
    {
        var ent = Spawn(baseProto);
        EnsureComp<DiseaseComponent>(ent, out var disease);
        disease.Complexity = complexity;
        disease.Genotype = _random.Next();
        MutateDisease(ent);
        return ent;
    }

    #endregion

}
