// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Shared.Buckle.Components;
using Content.Trauma.Shared.Temperature;

namespace Content.Trauma.Shared.SpaceImmunityOnBuckle;

public sealed partial class SpaceImmunityOnBuckleSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnBuckled(Entity<SpaceImmunityOnBuckleComponent> ent, ref StrappedEvent args)
    {
        ent.Comp.HadPressure = EnsureComp<PressureImmunityComponent>(args.Buckle.Owner, out _);
        ent.Comp.HadLowTemp = EnsureComp<SpecialLowTempImmunityComponent>(args.Buckle.Owner, out _);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<SpaceImmunityOnBuckleComponent> ent, ref UnstrappedEvent args)
    {
        if (!ent.Comp.HadPressure)
            RemComp<PressureImmunityComponent>(args.Buckle.Owner);
        if (!ent.Comp.HadLowTemp)
            RemComp<SpecialLowTempImmunityComponent>(args.Buckle.Owner);
    }
}
