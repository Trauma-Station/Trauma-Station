// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Trauma.Shared.Movement;

/// <summary>
/// Fixes movement status effects not refreshing your movespeed when added.
/// Youd need to equip clothing etc to refresh it manually.
/// </summary>
public sealed class MovementStatusFixSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!; // W

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementModStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
    }

    private void OnApplied(Entity<MovementModStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _speed.RefreshMovementSpeedModifiers(args.Target);
    }
}
