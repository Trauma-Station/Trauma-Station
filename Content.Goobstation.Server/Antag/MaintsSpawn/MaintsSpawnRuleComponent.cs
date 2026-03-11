// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Goobstation.Server.Antag.MaintsSpawn;

/// <summary>
/// This is used for the maints spawn rule
/// </summary>
[RegisterComponent]
public sealed partial class MaintsSpawnRuleComponent : Component
{
    /// <summary>
    /// Locations that was picked.
    /// </summary>
    [ViewVariables]
    public List<MapCoordinates>? Coords;
}
