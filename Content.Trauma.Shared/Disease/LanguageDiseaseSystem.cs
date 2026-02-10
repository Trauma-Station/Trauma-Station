// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Goobstation.Shared.Disease.Systems;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Disease;

/// <summary>
/// Relays <see cref="EntitySpokeEvent"/> to diseases and handles vocal parasite activation.
/// </summary>
public sealed class LanguageDiseaseSystem : EntitySystem
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseCarrierComponent, EntitySpokeEvent>(OnCarrierSpoke);

        SubscribeLocalEvent<LanguageDiseaseComponent, MapInitEvent>(OnDiseaseInit);
        SubscribeLocalEvent<LanguageDiseaseComponent, EntitySpokeEvent>(OnDiseaseSpoke);
        SubscribeLocalEvent<LanguageDiseaseComponent, DiseaseCloneEvent>(OnClonedInto);
    }

    private void OnCarrierSpoke(Entity<DiseaseCarrierComponent> ent, ref EntitySpokeEvent args)
    {
        foreach (var disease in ent.Comp.Diseases.ContainedEntities)
        {
            RaiseLocalEvent(disease, args);
        }
    }

    private void OnDiseaseInit(Entity<LanguageDiseaseComponent> ent, ref MapInitEvent args)
    {
        // start off dormant
        _disease.SetInfectionRate(ent.Owner, 0f);
    }

    private void OnDiseaseSpoke(Entity<LanguageDiseaseComponent> ent, ref EntitySpokeEvent args)
    {
        // ignore non-target languages
        var id = new ProtoId<LanguagePrototype>(args.Language.ID);
        if (ent.Comp.Languages.Contains(id) == ent.Comp.Inverted)
            return;

        // trigger the disease
        _disease.SetInfectionRate(ent.Owner, ent.Comp.TriggerInfectionRate);
    }

    private void OnClonedInto(Entity<LanguageDiseaseComponent> ent, ref DiseaseCloneEvent args)
    {
        var comp = EnsureComp<LanguageDiseaseComponent>(args.Cloned);
        comp.Languages = new(ent.Comp.Languages);
        comp.TriggerInfectionRate = ent.Comp.TriggerInfectionRate;
        comp.Inverted = ent.Comp.Inverted;
        Dirty(args.Cloned.Owner, comp);
    }

    // TODO: have some way with surgery to do the devils house and add to comp.languages
}
