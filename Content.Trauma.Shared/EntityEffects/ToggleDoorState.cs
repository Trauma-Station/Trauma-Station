// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class ToggleDoorState : EntityEffectBase<ToggleDoorState>
{
    [DataField]
    public bool Predicted = true;
}

public sealed partial class ToggleDoorStateEffectSystem : EntityEffectSystem<DoorComponent, ToggleDoorState>
{
    [Dependency] private SharedDoorSystem _door = default!;

    protected override void Effect(Entity<DoorComponent> ent, ref EntityEffectEvent<ToggleDoorState> args)
    {
        _door.TryToggleDoor(ent, ent.Comp, args.User, args.Effect.Predicted);
    }
}
