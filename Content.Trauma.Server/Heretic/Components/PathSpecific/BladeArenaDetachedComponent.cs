// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Server.Heretic.Components.PathSpecific;

[RegisterComponent]
public sealed partial class BladeArenaDetachedComponent : Component
{
    [DataField]
    public EntityCoordinates OriginalCoords;

    [DataField]
    public Angle OriginalRotation;
}
