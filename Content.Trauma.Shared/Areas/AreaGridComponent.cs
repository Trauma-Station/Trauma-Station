// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Stores areas on a grid efficiently for quick lookup and map saving/loading.
/// Basically the area version of <c>MapGridComponent</c>.
/// </summary>
[RegisterComponent, Access(typeof(MapAreaSystem))]
public sealed partial class AreaGridComponent : Component
{
    [DataField]
    public Dictionary<byte, EntProtoId> AreaMap = new();

    [DataField]
    public byte LastMapping;

    [DataField]
    public int ChunkSize = 16;

    /// <summary>
    /// Base64-encoded binary representation of areas in a chunk.
    /// Index 0 means no area, anything else is looked up in <see cref="AreaMap"/> then spawned at the tile position.
    /// Position in the data is <c>x + y * ChunkSize</c>, relative to the chunk position.
    /// </summary>
    [DataField]
    public Dictionary<Vector2i, AreaChunk> Chunks = new();
}

[DataDefinition]
public sealed partial class AreaChunk
{
    [DataField(required: true)]
    public string Data = string.Empty;

    /// <summary>
    /// Live area entities that are on this chunk.
    /// Only exists while the map is loaded, area entities do not get saved directly.
    /// Must have a length equal to chunk size squared.
    /// </summary>
    [ViewVariables]
    public EntityUid[] Areas = default!;

    /// <summary>
    /// How many area entities are currently live in this chunk.
    /// </summary>
    [ViewVariables]
    public int AreaCount;
}
