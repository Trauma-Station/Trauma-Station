// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Lavaland.Shared.Procedural.Prototypes;

/// <summary>
/// Contains information about Lavaland ruin configuration.
/// </summary>
[Prototype]
public sealed partial class LavalandDungeonRuinPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Vector2i Boundary;

    [DataField(required: true)]
    public EntProtoId SpawnedMarker;

    [DataField]
    public int SpawnAttempts = 8;

    [DataField(required: true)]
    public int Priority = int.MinValue;
}
