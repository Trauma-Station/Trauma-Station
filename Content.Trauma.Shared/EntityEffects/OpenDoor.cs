// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class OpenDoor : EntityEffectBase<OpenDoor>
{
    [DataField]
    public bool Value;
}

public sealed class OpenDoorEffectSystem : EntityEffectSystem<DoorComponent, OpenDoor>
{
    [Dependency] private readonly SharedDoorSystem _door = default!;

    protected override void Effect(Entity<DoorComponent> ent, ref EntityEffectEvent<OpenDoor> args)
    {
        _door.StartOpening(ent, ent.Comp, args.User, true);
    }
}
