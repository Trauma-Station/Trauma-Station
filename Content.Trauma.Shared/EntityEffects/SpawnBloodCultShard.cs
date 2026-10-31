// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Constructs.SoulShard;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

/// <summary>
/// Spawns cult shard
/// </summary>
public sealed partial class SpawnBloodCultShard : EntityEffectBase<SpawnBloodCultShard>;

public sealed partial class SpawnBloodCultShardEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnBloodCultShard>
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SoulShardSystem _shard = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnBloodCultShard> args)
    {
        if (_cult.GetRule(ent.Owner) is not { } rule)
            return;

        _shard.SpawnShard(entity.Comp.Coordinates, rule);
    }
}
