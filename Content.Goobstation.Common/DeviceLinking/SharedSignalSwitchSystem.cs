// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Common.DeviceLinking;

[Serializable, NetSerializable]
public enum SignalSwitchVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum SignalSwitchState : byte
{
    On,
    Off
}
