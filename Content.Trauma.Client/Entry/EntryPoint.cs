// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface;
using Content.Trauma.Client.Input;
using Content.Trauma.Client.IoC;
using Content.Trauma.Client.ItemSlotRenderer;
using Content.Trauma.Client.UserInterface;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.ContentPack;

namespace Content.Trauma.Client.Entry;

public sealed partial class EntryPoint : GameClient
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IOverlayManager _overlay = default!;

    public override void PreInit()
    {
        base.PreInit();

        ContentTraumaClientIoC.Register(Dependencies);
        IoCManager.InjectDependencies(this);
    }

    public override void PostInit()
    {
        base.PostInit();

        TraumaInputContexts.SetupContexts(_input.Contexts);

        _overlay.AddOverlay(new SpriteToLayerBullshitOverlay());
    }
}
