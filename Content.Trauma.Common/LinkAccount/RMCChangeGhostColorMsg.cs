// SPDX-License-Identifier: AGPL-3.0-or-later

using Lidgren.Network;

namespace Content.Trauma.Common.LinkAccount;

public sealed class RMCChangeGhostColorMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Color Color;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Color = buffer.ReadColor();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Color);
    }
}
