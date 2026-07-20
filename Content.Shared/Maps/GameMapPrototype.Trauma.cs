// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.Maps;

/// <summary>
/// Trauma - store lavaland planets for each map
/// </summary>
public sealed partial class GameMapPrototype
{
    /// <summary>
    /// Contains info about planets that we have to spawn assigned from this game map.
    /// Not protoid because its in lavaland.shared
    /// </summary>
    [DataField]
    public List<string> Planets = new() { "Lavaland" };

    /// <summary>
    /// The map pool's <c>RequiredEntities</c> that this map gets to leave out and still pass tests.
    /// These should have a good reason to not be mapped.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> IgnoredRequiredEntities = new();
}
