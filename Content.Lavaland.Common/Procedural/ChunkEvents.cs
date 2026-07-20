// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Common.Procedural;

/// <summary>
/// Raised on a grid entity when biome chunk is about to load.
/// Cancel to prevent loading it.
/// </summary>
[ByRefEvent]
public record struct ChunkLoadAttemptEvent(Vector2i Chunk, bool Cancelled = false);
