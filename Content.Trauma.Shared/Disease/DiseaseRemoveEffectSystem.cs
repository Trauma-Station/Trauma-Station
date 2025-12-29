// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Systems;

namespace Content.Trauma.Shared.Disease;

public sealed class DiseaseRemoveEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRemoveEffectComponent, DiseaseEffectEvent>(OnRemoveEffect);
    }

    private void OnRemoveEffect(Entity<DiseaseRemoveEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        _disease.TryCure(args.Ent.AsNullable(), args.Disease);
    }
}
