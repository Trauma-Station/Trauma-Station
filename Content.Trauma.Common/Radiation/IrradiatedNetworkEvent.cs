namespace Content.Trauma.Common.Radiation;

/// <summary>
/// Raised on server as networked event when radiation system updates its state
/// This is kinda evil but I'd rather do it this way than refactor the whole radiation system
/// </summary>
/// <remarks>
/// I suck at event names
/// </remarks>
[Serializable, NetSerializable]
public sealed class IrradiatedNetworkEvent(float rads) : EntityEventArgs
{
    public float Rads = rads;
}
