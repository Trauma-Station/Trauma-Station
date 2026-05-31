// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;

namespace Content.Trauma.Common.Mentor;

public sealed class MentorStatusMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public bool IsMentor;
    public bool CanReMentor;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsMentor = buffer.ReadBoolean();
        CanReMentor = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsMentor);
        buffer.Write(CanReMentor);
    }
}
