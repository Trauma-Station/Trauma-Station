using System;
using System.Collections.Generic;
using System.Text;
using Content.White.Client.ItemSlotRenderer;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;

namespace Content.White.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void PostInit()
    {
        base.PostInit();

        _overlay.AddOverlay(new SpriteToLayerBullshitOverlay());
    }
}
