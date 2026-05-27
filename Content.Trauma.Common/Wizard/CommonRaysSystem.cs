// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Map;

namespace Content.Trauma.Common.Wizard;

public abstract class CommonRaysSystem : EntitySystem
{
    public abstract EntityUid? DoRays(MapCoordinates coords,
        Color colorA,
        Color colorB,
        int min = 5,
        int max = 10,
        Vector2? minMaxRadius = null,
        Vector2? minMaxEnergy = null,
        string proto = "EffectRay",
        bool server = true);
}
