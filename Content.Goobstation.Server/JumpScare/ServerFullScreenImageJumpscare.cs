// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.JumpScare;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Goobstation.Server.JumpScare;

public sealed partial class ServerFullScreenImageJumpscare : IFullScreenImageJumpscare, IPostInjectInit
{
    [Dependency] private INetManager _netManager = default!;

    public void Jumpscare(SpriteSpecifier.Texture image, ICommonSession? session)
    {
        DebugTools.Assert(session is not null);
        Jumpscare(session.Channel, image.TexturePath);
    }

    private void Jumpscare(INetChannel channel, ResPath imagepath)
    {
        var msg = new JumpscareMessage()
        {
            ImagePath = imagepath.CanonPath,
        };

        _netManager.ServerSendMessage(msg, channel);
    }

    public void PostInject()
    {
        RegisterNetMessages();
    }

    private void RegisterNetMessages()
    {
        _netManager.RegisterNetMessage<JumpscareMessage>();
    }
}
