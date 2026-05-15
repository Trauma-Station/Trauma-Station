// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Silicon.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CommanderComponent : Component
{
    /// <summary>
    /// What are we using as our waypoint?
    /// </summary>
    [DataField]
    public EntProtoId WaypointEntityUid = "SecuritronWaypoint";

    /// <summary>
    /// A list of waypoints placed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Waypoints { get; set; } = new();

    /// <summary>
    /// Should slaves be patrolling?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsPatrolling;

    /// <summary>
    /// Which sound to play on enslavement.
    /// </summary>
    [DataField]
    public SoundSpecifier EnslaveSound = new SoundPathSpecifier("/Audio/Machines/chime.ogg");

}

[ByRefEvent]
public sealed partial class TogglePatrolActionEvent : InstantActionEvent;

public sealed partial class WaypointActionEvent : WorldTargetActionEvent;

[ByRefEvent]
public sealed partial class ClearWaypointsActionEvent : InstantActionEvent;
