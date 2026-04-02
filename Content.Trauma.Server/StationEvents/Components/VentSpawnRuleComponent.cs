// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class VentSpawnRuleComponent : Component
{
    /// <summary>
    /// Locations that was picked.
    /// </summary>
    [ViewVariables]
    public List<MapCoordinates>? Coords;
}
