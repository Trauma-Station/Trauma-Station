// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;
using Robust.Client.Graphics;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class DigitalCamouflageSystem : SharedDigitalCamouflageSystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new DigitalCamouflageOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<DigitalCamouflageOverlay>();
    }
}
