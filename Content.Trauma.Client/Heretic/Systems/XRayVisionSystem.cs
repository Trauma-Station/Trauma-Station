// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Crucible.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class XRayVisionSystem : SharedXRayVisionSystem
{
    [Dependency] private ILightManager _light = default!;
    [Dependency] private IPlayerManager _player = default!;

    protected override void DrawLight(EntityUid uid, bool value)
    {
        base.DrawLight(uid, value);

        if (_player.LocalEntity != uid)
            return;

        _light.DrawLighting = value;
    }
}
