// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Buckle.Components;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.Temperature;

namespace Content.Trauma.Shared.SpaceImmunityOnBuckle;

public sealed partial class SpaceImmunityOnBuckleSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly EntProtoId PressureImmunity = "StatusEffectPressureImmunityBuckle";

    [SubscribeLocalEvent]
    private void OnBuckled(Entity<SpaceImmunityOnBuckleComponent> ent, ref StrappedEvent args)
    {
        _status.TrySetStatusEffectDuration(args.Buckle.Owner, PressureImmunity);
        ent.Comp.HadLowTemp = EnsureComp<SpecialLowTempImmunityComponent>(args.Buckle.Owner, out _);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<SpaceImmunityOnBuckleComponent> ent, ref UnstrappedEvent args)
    {
        _status.TryRemoveStatusEffect(args.Buckle.Owner, PressureImmunity);
        if (!ent.Comp.HadLowTemp)
            RemComp<SpecialLowTempImmunityComponent>(args.Buckle.Owner);
    }
}
