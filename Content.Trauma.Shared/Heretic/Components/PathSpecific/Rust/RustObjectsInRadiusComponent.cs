// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class RustObjectsInRadiusComponent : Component
{
    [DataField]
    public float RustRadius = 1.5f;

    [DataField]
    public float LookupRange = 0.1f;

    [DataField]
    public int RustStrength = 10;

    [DataField]
    public EntProtoId TileRune = "TileHereticRustRune";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRustTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan RustPeriod = TimeSpan.FromSeconds(0.1);
}
