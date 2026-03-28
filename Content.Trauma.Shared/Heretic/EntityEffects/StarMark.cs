// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

public sealed partial class StarMark : EntityEffectBase<StarMark>;

public sealed class StarMarkEffectSystem : EntityEffectSystem<MobStateComponent, StarMark>
{
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;

    protected override void Effect(Entity<MobStateComponent> ent, ref EntityEffectEvent<StarMark> args)
    {
        _starMark.TryApplyStarMark(ent.AsNullable());
    }
}
