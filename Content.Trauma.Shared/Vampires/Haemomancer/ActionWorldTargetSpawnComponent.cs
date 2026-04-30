// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Actions;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

/// <summary>
/// Action component that spawns an entity prototype around a 3x3 area of a tile.
/// TODO: Move this outta here
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionWorldTargetSpawnComponent : Component
{
    /// <summary>
    /// The prototype to spawn
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnPrototype;

    /// <summary>
    /// The size of the area
    /// </summary>
    [DataField]
    public Vector2 Size = new(3, 3);

    /// <summary>
    /// If set, the spawning algorithm will add this offset when iterating over the possible positions.
    /// </summary>
    [DataField]
    public Vector2i Offset;

    /// <summary>
    /// Whether to predict the spawning.
    /// </summary>
    [DataField]
    public bool Predicted = true;
}

public sealed partial class WorldTargetSpawnActionEvent : WorldTargetActionEvent;
