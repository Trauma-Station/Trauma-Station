// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Administration;

[Serializable, NetSerializable]
public sealed class AdminInfoEvent(NetUserId userid) : EntityEventArgs
{
    public NetUserId user = userid;
}
