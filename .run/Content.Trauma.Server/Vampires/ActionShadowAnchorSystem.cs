// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Vampires.Umbrae;
using Robust.Shared.Map;

namespace Content.Trauma.Server.Vampires;

public sealed partial class ActionShadowAnchorSystem : SharedActionShadowAnchorSystem
{
    [Dependency] private VampireUmbraeSystem _umbrae = default!;

    protected override void SpawnShadowClone(EntityUid uid, MapCoordinates coordinates)
    {
        base.SpawnShadowClone(uid, coordinates);

        _umbrae.SpawnShadowClones(uid, coordinates);
    }
}
