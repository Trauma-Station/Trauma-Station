// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Goobstation.Shared.Smites;

public sealed class InputSwapSystem : ToggleableSmiteSystem<InputSwapComponent>
{
    [Dependency] private readonly MovementSpeedModifierSystem _move = default!;

    public override void Set(EntityUid ent)
    {
        if (!TryComp<MovementSpeedModifierComponent>(ent, out var mod))
            return; // womp

        _move.ChangeBaseSpeed(ent, -mod.BaseWalkSpeed, -mod.BaseSprintSpeed, mod.Acceleration);
        _move.RefreshMovementSpeedModifiers(ent);
    }
}
