// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Crucible.Systems;
using Robust.Client.Graphics;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class XRayVisionSystem : SharedXRayVisionSystem
{
    [Dependency] private readonly ILightManager _light = default!;

    protected override void DrawLight(bool value)
    {
        base.DrawLight(value);

        _light.DrawLighting = value;
    }
}
