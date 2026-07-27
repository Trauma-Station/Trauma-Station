// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Bitrunning;

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
