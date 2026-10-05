// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CCVar;
using Content.Trauma.Shared.Radiation;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Radiation;

public sealed partial class RadiationOverlaySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IGameTiming _timing = default!;

    private RadiationOverlay _overlay = default!;
    private TimeSpan _lastUpdate;
    private TimeSpan _updateRate;
    private float _lastValue;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
        Subs.CVar(_cfg, CCVars.RadiationGridcastUpdateRate, updateRate =>
            _updateRate = TimeSpan.FromSeconds(updateRate + 0.2f), true); // Extra 200ms of delay before the overlay is removed to compensate for network delay changes
    }

    public override void Update(float frameTime)
    {
        if (_lastValue == 0) return;

        if (_timing.CurTime < _lastUpdate + _updateRate)
        {
            // If overlay is active, smoothly change the effect's intensity, because radiation system only updates once per second
            _overlay.Radiation += (_lastValue - _overlay.Radiation) * frameTime * 0.2f;
            return;
        }

        // If no new OnIrradiatedEvent is recieved, remove the noise, as we are no longer being irradiated... or maybe the network just died. Woops.
        _overlay.Radiation = 0;
        _lastValue = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    /// <summary>
    /// Updates the overlay whenever the local player gets irradiated.
    /// </summary>
    [SubscribeNetworkEvent]
    private void OnIrradiated(IrradiatedNetworkEvent args)
    {
        if (_lastValue == 0)
        {
            _overlayMan.AddOverlay(_overlay);
        }
        _lastValue = args.Rads;
        _lastUpdate = _timing.CurTime;
    }
}
