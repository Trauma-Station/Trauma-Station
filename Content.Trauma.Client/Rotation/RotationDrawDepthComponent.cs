// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Client.Rotation;

[RegisterComponent]
public sealed partial class RotationDrawDepthComponent : Component
{
    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepth>))]
    public int DefaultDrawDepth;

    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepth>))]
    public int SouthDrawDepth;
}
