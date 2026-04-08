// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.ClockworkCult.Power.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Trauma.Client.ClockworkCult;

/// <summary>
/// This handles turning the <see cref="ClockworkTransferOverlay"/> on/off.
/// </summary>
public sealed class ClockwinderSystem : SharedClockwinderSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void ToggleOverlay()
    {
        base.ToggleOverlay();

        if (_player.LocalEntity == null)
            return;

        if (!_overlay.HasOverlay<ClockworkTransferOverlay>())
        {
            _overlay.AddOverlay(new ClockworkTransferOverlay());
            return;
        }

        _overlay.RemoveOverlay<ClockworkTransferOverlay>();
    }
}
