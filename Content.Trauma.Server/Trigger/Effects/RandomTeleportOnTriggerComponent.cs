// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Destructible.Thresholds;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio;


namespace Content.Trauma.Server.Trigger.Effects;

/// <summary>
/// Teleportation component to perform RandomTeleport on trigger.
/// </summary>
[RegisterComponent, Access(typeof(RandomTeleportOnTriggerSystem))]
public sealed partial class RandomTeleportOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public MinMax TeleportationRadius = new(3, 6);

    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
