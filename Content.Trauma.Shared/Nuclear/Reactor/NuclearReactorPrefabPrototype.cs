// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear.Reactor;

/// <summary>
/// A premade configuration for a nuclear reactor's parts grid.
/// </summary>
[Prototype]
public sealed partial class NuclearReactorPrefabPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Dictionary<Vector2i, EntProtoId> Parts = new();
}
