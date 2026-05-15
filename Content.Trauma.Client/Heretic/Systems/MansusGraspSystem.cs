// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Systems;
using Robust.Client.Graphics;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class MansusGraspSystem : SharedMansusGraspSystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new AreaMansusGraspOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<AreaMansusGraspOverlay>();
    }
}
