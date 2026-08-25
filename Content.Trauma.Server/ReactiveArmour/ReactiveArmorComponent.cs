// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;


namespace Content.Server.Trauma.ReactiveArmour;

/// <summary>
/// Component that is given to mob upon wearing reactive armour
/// </summary>
[RegisterComponent]
public sealed partial class ReactiveArmourComponent : Component
{
    [DataField]
    public string ArmourBehavior;

    [DataField]
    public float ActivationChance = .5f;

    // Teleport
    [DataField]
    public MinMax TeleportationRadius = new(3, 6);

    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [DataField]
    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    // Tesla
    [DataField]
    public float LightningRange = 7f;

    [DataField]
    public int LightningBoltCount = 5;

    // Cloack



    // Repulsive
}
