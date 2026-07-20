// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Client.Fishing.Overlays;
using Content.Goobstation.Shared.Fishing.Systems;
using Robust.Client.Player;

namespace Content.Goobstation.Client.Fishing;

public sealed partial class FishingSystem : SharedFishingSystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new FishingOverlay(EntityManager, _player));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<FishingOverlay>();
    }
}
