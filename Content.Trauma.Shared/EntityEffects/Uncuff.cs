// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that uncuffs an entity instantly.
/// </summary>
public sealed partial class Uncuff : EntityEffectBase<Uncuff>;

public sealed partial class UncuffEffectSystem : EntityEffectSystem<CuffableComponent, Uncuff>
{
    [Dependency] private SharedCuffableSystem _cuff = default!;

    protected override void Effect(Entity<CuffableComponent> ent, ref EntityEffectEvent<Uncuff> args)
    {
        if (!_cuff.TryGetLastCuff(ent.AsNullable(), out var lastCuffs) || lastCuffs is not { } cuffs )
            return;

        _cuff.Uncuff(ent.AsNullable(), ent.Owner, cuffs);
    }
}
