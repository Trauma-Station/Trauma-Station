// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Systems.Side;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class UnfathomableCurioSystem : SharedUnfathomableCurioSystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new CurioShieldOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<CurioShieldOverlay>();
    }
}
