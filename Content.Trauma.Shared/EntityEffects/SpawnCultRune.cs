// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that spawns ent snapped to grid and gives it cult rule.
/// </summary>
public sealed partial class SpawnCultRune : BaseSpawnEntityEntityEffect<SpawnCultRune>;

public sealed partial class SpawnCultRuneEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnCultRune>
{
    [Dependency] private BloodCultSystem _cult = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnCultRune> args)
    {
        var pos = ent.Comp.Coordinates.SnapToGrid(EntityManager);

        _cult.TrySpawnRune(ent.Owner, pos, rune);
    }
}
