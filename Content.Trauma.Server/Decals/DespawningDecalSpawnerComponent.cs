// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Decals;

/// <summary>
/// Component for random decal spawner entities to queue despawning their spawned decals.
/// </summary>
[RegisterComponent]
public sealed partial class DespawningDecalSpawnerComponent : Component;
