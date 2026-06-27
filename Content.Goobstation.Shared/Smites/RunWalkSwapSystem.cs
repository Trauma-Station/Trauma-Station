// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Components;

namespace Content.Goobstation.Shared.Smites;

public sealed partial class RunWalkSwapSystem : ToggleableSmiteSystem<RunWalkSwapComponent>
{
    public override void Set(EntityUid owner)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(owner);
        (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) = (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);
    }
}
