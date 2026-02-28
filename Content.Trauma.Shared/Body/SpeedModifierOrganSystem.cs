// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Trauma.Shared.Body;

public sealed class SpeedModifierOrganSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedModifierOrganComponent, OrganEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<SpeedModifierOrganComponent, OrganDisabledEvent>(OnDisabled);
    }

    private void OnEnabled(Entity<SpeedModifierOrganComponent> ent, ref OrganEnabledEvent args)
    {
        var comp = EnsureComp<MovementSpeedModifierComponent>(args.Body);
        comp.BaseWeightlessAcceleration += ent.Comp.WeightlessAcceleration;
        Dirty(args.Body, comp);
        _movement.RefreshWeightlessModifiers(ent.Owner);
    }

    private void OnDisabled(Entity<SpeedModifierOrganComponent> ent, ref OrganDisabledEvent args)
    {
        if (TerminatingOrDeleted(args.Body) || !TryComp<MovementSpeedModifierComponent>(args.Body, out var comp))
            return;

        comp.BaseWeightlessAcceleration -= ent.Comp.WeightlessAcceleration;
        Dirty(args.Body, comp);
        _movement.RefreshWeightlessModifiers(ent.Owner);
    }
}
