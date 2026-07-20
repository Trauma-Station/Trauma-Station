// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Server.RandomChanceSpawner;

/// <summary>
/// Shitcode my beloved
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class RandomChanceSpawnerComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, float> ToSpawn = new();
}
