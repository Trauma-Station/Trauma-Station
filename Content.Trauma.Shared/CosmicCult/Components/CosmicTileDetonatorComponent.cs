// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's entropic colossus. Currently unused.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicTileDetonatorComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DetonationTimer = default!;

    [DataField] public EntProtoId TileDetonation = "MobTileDamageArea";

    [DataField] public TimeSpan DetonateWait = TimeSpan.FromSeconds(0.525);

    [DataField] public Vector2i DetonationCenter;

    [DataField] public Vector2 MaxSize = new(8, 8);

    [DataField] public Vector2 Size = new(0, 0);
}
