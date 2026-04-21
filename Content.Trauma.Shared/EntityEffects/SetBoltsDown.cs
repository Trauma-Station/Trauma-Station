// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class SetBoltsDown : EntityEffectBase<SetBoltsDown>
{
    [DataField]
    public bool Value;
}

public sealed class SetBoltsDownEffectSystem : EntityEffectSystem<DoorBoltComponent, SetBoltsDown>
{
    [Dependency] private readonly SharedDoorSystem _door = default!;

    protected override void Effect(Entity<DoorBoltComponent> ent, ref EntityEffectEvent<SetBoltsDown> args)
    {
        _door.SetBoltsDown(ent, args.Effect.Value, args.User, true);
    }
}
