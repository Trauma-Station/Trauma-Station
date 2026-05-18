// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Silicon.Components;

/// <summary>
/// Used by wandsky system to assign sec bots to patrols.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PatrolCommanderComponent : Component
{
    /// <summary>
    /// Waypoint ProtoId.
    /// </summary>
    [DataField]
    public EntProtoId WaypointId = "SecuritronWaypoint";

    /// <summary>
    /// List of waypoints placed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Waypoints = new();

    /// <summary>
    /// Should slaved robots be patrolling?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsPatrolling;

    /// <summary>
    /// Which sound to play on enslavement.
    /// </summary>
    [DataField]
    public SoundSpecifier EnslaveSound = new SoundPathSpecifier("/Audio/Machines/chime.ogg");
}

public sealed partial class TogglePatrolActionEvent : InstantActionEvent;

public sealed partial class WaypointActionEvent : WorldTargetActionEvent;

public sealed partial class ClearWaypointsActionEvent : InstantActionEvent;
