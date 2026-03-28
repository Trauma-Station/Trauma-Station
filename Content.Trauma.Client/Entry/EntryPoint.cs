// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shitcode.Common.LinkAccount;
using Content.Trauma.Client.IoC;
using Content.Trauma.Client.ItemSlotRenderer;
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

        IoCManager.Register<ILinkAccountManager, LinkAccountManager>();
    }

    public override void PostInit()
    {
        _overlay.AddOverlay(new SpriteToLayerBullshitOverlay());
    }
}
