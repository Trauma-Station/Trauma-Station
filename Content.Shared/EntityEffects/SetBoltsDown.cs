using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Shared.EntityEffects;

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
