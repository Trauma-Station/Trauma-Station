// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class AreaSpawnerComponent : Component
{
    [DataField]
    public int Radius = 3;

    [DataField]
    public EntProtoId SpawnPrototype;

    [DataField]
    public TimeSpan SpawnDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public float MinTime = 1f;

    [DataField]
    public float MaxTime = 5f;

    [ViewVariables]
    public TimeSpan SpawnAt = TimeSpan.Zero;

    [ViewVariables]
    public List<EntityUid> Spawneds = new List<EntityUid>();
}
