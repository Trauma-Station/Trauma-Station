// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.Vampires;
using Content.Trauma.Shared.EntityEffects.Vampires;

namespace Content.Trauma.Server.EntityEffects;

public sealed class SpawnShadowCloneEffectSystem : SharedSpawnShadowCloneEffectSystem
{
    [Dependency] private VampireUmbraeSystem _umbrae = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void SpawnShadowClones(EntityUid original, int amount)
    {
        base.SpawnShadowClones(original, amount);

        var mapCoords = _transform.GetMapCoordinates(original);
        _umbrae.SpawnShadowClones(original, mapCoords, amount);
    }
}
