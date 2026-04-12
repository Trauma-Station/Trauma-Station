// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Decals;

/// <summary>
/// Event raised on a random decal spawner when spawning a decal.
/// </summary>
[ByRefEvent]
public record struct DecalSpawnedEvent(EntityUid Grid, uint Decal);
