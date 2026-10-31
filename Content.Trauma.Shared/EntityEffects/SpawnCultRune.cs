// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.BloodCult.Runes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that spawns ent snapped to grid and gives it cult rule.
/// </summary>
public sealed partial class SpawnCultRune : EntityEffectBase<SpawnCultRune>
{
    [DataField(required: true)]
    public ProtoId<BloodRunePrototype> Rune;
}

public sealed partial class SpawnCultRuneEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnCultRune>
{
    [Dependency] private CultRuneSystem _rune = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnCultRune> args)
    {
        if (!ProtoMan.TryIndex(args.Effect.Rune, out var rune))
            return;

        var pos = ent.Comp.Coordinates.SnapToGrid(EntityManager);

        _rune.TrySpawnRune(ent.Owner, pos, rune);
    }
}
