// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

public sealed partial class GameMapPoolPrototype
{
    /// <summary>
    /// Areas that must be mapped on every map in this pool, or tests will fail.
    /// </summary>
    [DataField]
    public List<EntProtoId> RequiredAreas = new();

    /// <summary>
    /// Entities that must be mapped on every map in this pool, or tests will fail.
    /// Can be ignored per-map using <c>IgnoredRequiredEntities</c>.
    /// </summary>
    [DataField]
    public List<EntProtoId> RequiredEntities = new();
}
