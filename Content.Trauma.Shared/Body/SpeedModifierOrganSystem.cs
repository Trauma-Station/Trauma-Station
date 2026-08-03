// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Trauma.Shared.Body;

public sealed partial class SpeedModifierOrganSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    [SubscribeLocalEvent]
    private void OnEnabled(Entity<SpeedModifierOrganComponent> ent, ref OrganEnabledEvent args)
    {
        var comp = EnsureComp<MovementSpeedModifierComponent>(args.Body);
        comp.BaseWeightlessAcceleration += ent.Comp.WeightlessAcceleration;
        comp.BaseWeightlessModifier += ent.Comp.WeightlessModifier;
        comp.BaseWeightlessFriction += ent.Comp.WeightlessFriction;
        Dirty(args.Body, comp);
        _movement.RefreshWeightlessModifiers(args.Body);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<SpeedModifierOrganComponent> ent, ref OrganDisabledEvent args)
    {
        if (TerminatingOrDeleted(args.Body) || !TryComp<MovementSpeedModifierComponent>(args.Body, out var comp))
            return;

        comp.BaseWeightlessAcceleration -= ent.Comp.WeightlessAcceleration;
        comp.BaseWeightlessModifier -= ent.Comp.WeightlessModifier;
        comp.BaseWeightlessFriction -= ent.Comp.WeightlessFriction;
        Dirty(args.Body, comp);
        _movement.RefreshWeightlessModifiers(args.Body);
    }
}
