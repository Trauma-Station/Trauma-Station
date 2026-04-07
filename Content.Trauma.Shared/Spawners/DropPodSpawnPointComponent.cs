// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Spawners;

/// <summary>
/// Roundstart spawnpoint that puts spawned players inside of a drop pod.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DropPodSpawnPointComponent : Component;
