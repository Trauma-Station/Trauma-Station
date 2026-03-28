using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

namespace Content.Shared.EntityEffects;

public sealed partial class ClearAccesses : EntityEffectBase<ClearAccesses>;

public sealed class ClearAccessesEffectSystem : EntityEffectSystem<AccessReaderComponent, ClearAccesses>
{
    [Dependency] private readonly AccessReaderSystem _reader = default!;

    protected override void Effect(Entity<AccessReaderComponent> entity, ref EntityEffectEvent<ClearAccesses> args)
    {
        _reader.TryClearAccesses(entity);
    }
}
