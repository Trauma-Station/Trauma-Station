// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.Forensics.Components;

namespace Content.Trauma.Shared.Disease;

/// <summary>
/// Handles everything related to DNA targeting diseases.
/// </summary>
public sealed class DnaTargetDiseaseSystem : EntitySystem
{
    private EntityQuery<DnaComponent> _dnaQuery;
    private EntityQuery<DnaTargetDiseaseComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _dnaQuery = GetEntityQuery<DnaComponent>();
        _query = GetEntityQuery<DnaTargetDiseaseComponent>();

        SubscribeLocalEvent<DnaTargetDiseaseComponent, DiseaseGainedEvent>(OnDiseaseGained);
        SubscribeLocalEvent<DnaTargetDiseaseComponent, DiseaseCloneEvent>(OnClonedInto);

        SubscribeLocalEvent<DiseaseDnaTargetConditionComponent, DiseaseCheckConditionsEvent>(OnCheckConditions);
    }

    private void OnDiseaseGained(Entity<DnaTargetDiseaseComponent> ent, ref DiseaseGainedEvent args)
    {
        if (ent.Owner != args.Disease.Owner || _dnaQuery.CompOrNull(args.Carrier)?.DNA is not {} dna)
            return;

        // TODO: mgs4 parity from mutating
        if (!ent.Comp.TargetDnas.Contains(dna))
            return;

        ent.Comp.Enabled = true;
        Dirty(ent);
    }

    private void OnClonedInto(Entity<DnaTargetDiseaseComponent> ent, ref DiseaseCloneEvent args)
    {
        var comp = EnsureComp<DnaTargetDiseaseComponent>(args.Cloned);
        comp.TargetDnas = new(ent.Comp.TargetDnas);
    }

    private void OnCheckConditions(Entity<DiseaseDnaTargetConditionComponent> ent, ref DiseaseCheckConditionsEvent args)
    {
        if (_query.CompOrNull(args.Disease)?.Enabled != true)
            args.DoEffect = false;
    }

    public void AddTargetDnas(EntityUid disease, HashSet<string> dnas)
    {
        var comp = Comp<DnaTargetDiseaseComponent>(disease);
        comp.TargetDnas.UnionWith(dnas);
    }
}
