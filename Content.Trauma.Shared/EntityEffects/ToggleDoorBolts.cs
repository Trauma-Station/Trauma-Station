// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class ToggleDoorBolts : EntityEffectBase<ToggleDoorBolts>
{
    [DataField]
    public bool Predicted = true;
}

public sealed partial class ToggleDoorBoltsEffectSystem : EntityEffectSystem<DoorBoltComponent, ToggleDoorBolts>
{
    [Dependency] private SharedDoorSystem _door = default!;

    protected override void Effect(Entity<DoorBoltComponent> ent, ref EntityEffectEvent<ToggleDoorBolts> args)
    {
        _door.SetBoltsDown(ent, !ent.Comp.BoltsDown, args.User, args.Effect.Predicted);
    }
}
