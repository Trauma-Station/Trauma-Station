// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;

namespace Content.Trauma.Common.LinkAccount;

public sealed class RMCClearGhostColorMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
    }
}
