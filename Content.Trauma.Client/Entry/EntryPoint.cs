// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface;
using Content.Trauma.Client.IoC;
using Content.Trauma.Client.ItemSlotRenderer;
using Content.Trauma.Client.LinkAccount;
using Content.Trauma.Client.UserInterface;
using Content.Trauma.Common.LinkAccount;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;

namespace Content.Trauma.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void PreInit()
    {
        base.PreInit();

        ContentTraumaClientIoC.Register(Dependencies);
    }

    public override void Init()
    {
        base.Init();

        IoCManager.Register<ILinkAccountManagerClient, LinkAccountManager>();
        IoCManager.Register<IUserActionPanelManager, UserActionPanelManager>();
    }

    public override void PostInit()
    {
        _overlay.AddOverlay(new SpriteToLayerBullshitOverlay());
    }
}
