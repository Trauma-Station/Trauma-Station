// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radiation.Events;
using Content.Trauma.Shared.Radiation;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Radiation;

public sealed partial class RadiationOverlaySystem : EntitySystem
{
    /// <summary>
    /// When a player entity is irradiated, send them a network message to update their radiation visuals.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnIrradiated(Entity<ActorComponent> ent, ref OnIrradiatedEvent args)
    {
        var msg = new IrradiatedNetworkEvent(args.RadsPerSecond);
        RaiseNetworkEvent(msg, ent.Comp.PlayerSession);
    }
}
