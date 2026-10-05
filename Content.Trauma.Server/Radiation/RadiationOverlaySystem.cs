// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radiation.Events;
using Content.Trauma.Shared.Radiation;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Radiation;

public sealed partial class RadiationOverlaySystem : EntitySystem
{
    /// <summary>
    /// Updates the overlay whenever the local player gets irradiated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnIrradiated(Entity<ActorComponent> ent, ref OnIrradiatedEvent args)
    {
        var msg = new IrradiatedNetworkEvent(args.RadsPerSecond);
        RaiseNetworkEvent(msg, ent.Comp.PlayerSession);
    }
}
