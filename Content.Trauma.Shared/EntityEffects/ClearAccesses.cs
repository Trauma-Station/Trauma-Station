// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class ClearAccesses : EntityEffectBase<ClearAccesses>;

public sealed partial class ClearAccessesEffectSystem : EntityEffectSystem<AccessReaderComponent, ClearAccesses>
{
    [Dependency] private AccessReaderSystem _reader = default!;

    protected override void Effect(Entity<AccessReaderComponent> entity, ref EntityEffectEvent<ClearAccesses> args)
    {
        _reader.TryClearAccesses(entity);
    }
}
