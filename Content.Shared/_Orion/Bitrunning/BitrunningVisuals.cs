using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum QuantumServerVisualState : byte
{
    Unpowered,
    Ready,
    Cooling,
    Running,
}

[Serializable, NetSerializable]
public enum QuantumServerVisuals : byte
{
    QuantumServerState,
}

[Serializable, NetSerializable]
public enum ByteforgeVisuals : byte
{
    ByteforgePowered,
    ByteforgeActive,
    ByteforgeAngry,
}
