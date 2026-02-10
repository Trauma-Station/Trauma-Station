// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Spawners;

/// <summary>
/// Raised on a timed spawner after it spawns an entity.
/// </summary>
[ByRefEvent]
public record struct SpawnerSpawnedEvent(EntityUid Spawned);
