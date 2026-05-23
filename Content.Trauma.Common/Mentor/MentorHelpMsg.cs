// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;

namespace Content.Trauma.Common.Mentor;

public sealed class MentorHelpMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public string Message = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Message = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Message);
    }
}
