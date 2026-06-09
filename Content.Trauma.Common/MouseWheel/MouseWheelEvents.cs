namespace Content.Trauma.Common.MouseWheel;

[Serializable, NetSerializable]
public sealed class RotateCameraEvent(Angle rotation) : EntityEventArgs
{
    public Angle Rotation = rotation;
}
