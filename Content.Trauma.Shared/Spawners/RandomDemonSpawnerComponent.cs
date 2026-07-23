// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Spawners;

/// <summary>
/// Changes the spawned entity to be a random demon.
/// Makes the spawned demon a familiar if rolling <see cref="HostileChance"/> fails, the master is set by construction.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomDemonSpawnerComponent : Component
{
    /// <summary>
    /// Chance of a demon ghost role being hostile instead of a familiar for the summoner.
    /// If the spawner times out and force spawns the demon, it will always be hostile.
    /// </summary>
    [DataField]
    public float HostileChance = 0.5f;

    /// <summary>
    /// Possible demons to pick from.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Demons = new();
}
