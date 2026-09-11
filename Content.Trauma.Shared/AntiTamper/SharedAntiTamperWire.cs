// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AntiTamper;

[Serializable, NetSerializable]
public enum AntiTamperWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}
