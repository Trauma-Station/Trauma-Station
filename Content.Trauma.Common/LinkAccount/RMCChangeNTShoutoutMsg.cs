// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;

namespace Content.Trauma.Common.LinkAccount;

public sealed class RMCChangeNTShoutoutMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string? Name;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        if (buffer.PeekStringSize() > 10000)
            return;

        Name = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Name);
    }
}
