// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.MouseWheel;

[Serializable, NetSerializable]
public sealed class RotateCameraEvent(Angle rotation) : EntityEventArgs
{
    public Angle Rotation = rotation;
}
