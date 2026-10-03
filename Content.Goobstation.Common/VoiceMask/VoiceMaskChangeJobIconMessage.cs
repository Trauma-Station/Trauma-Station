// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeJobIconMessage(string jobIcon) : BoundUserInterfaceMessage
{
    public readonly string JobIcon = jobIcon;
}
