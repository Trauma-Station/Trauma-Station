// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class VoidConduitOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new VoidConduitOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<VoidConduitOverlay>();
    }
}
