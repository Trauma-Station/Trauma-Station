// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear;

/// <summary>
/// Component shared by nuclear reactors and turbines.
/// Manages the pipes and user logging.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class NuclearMachineComponent : Component
{
    /// <summary>
    /// Name of the pipe node
    /// </summary>
    [DataField]
    public string PipeName = "pipe";

    /// <summary>
    /// Inlet entity
    /// </summary>
    [DataField]
    public EntityUid? InletEnt;

    /// <summary>
    /// Position of the inlet entity
    /// </summary>
    [DataField]
    public Vector2 InletPos = new(-2, -1);

    /// <summary>
    /// Rotation of the inlet entity, in degrees
    /// </summary>
    [DataField]
    public float InletRot = -90;

    /// <summary>
    /// Outlet entity
    /// </summary>
    [DataField]
    public EntityUid? OutletEnt;

    /// <summary>
    /// Position of the outlet entity
    /// </summary>
    [DataField]
    public Vector2 OutletPos = new(2, 1);

    /// <summary>
    /// Rotation of the outlet entity, in degrees
    /// </summary>
    [DataField]
    public float OutletRot = 90;

    /// <summary>
    /// Name of the prototype of the arrows that indicate flow on client examine.
    /// </summary>
    [DataField]
    public EntProtoId ArrowPrototype = "NuclearMachineFlowArrow";

    /// <summary>
    /// Name of the prototype of the pipes the reactor uses to connect to the pipe network
    /// </summary>
    [DataField]
    public EntProtoId PipePrototype = "NuclearMachineGasPipe";

    /// <summary>
    /// The last user that changed a value.
    /// </summary>
    [ViewVariables]
    public EntityUid LastUser;

    /// <summary>
    /// The last monitor that changed a value, or null if it was itself.
    /// </summary>
    [ViewVariables]
    public EntityUid? LastMonitor;

    /// <summary>
    /// When to next log data for the machine.
    /// </summary>
    [AutoPausedField]
    public TimeSpan? NextLog;
}

/// <summary>
/// Raised on the machine to log its specific data after not being changed for a bit.
/// Monitor is the same as the machine the event is being raised on if no monitor was used.
/// </summary>
[ByRefEvent]
public record struct NuclearMachineLogEvent(EntityUid User, EntityUid Monitor);

[Serializable, NetSerializable]
public abstract class NuclearMachineBUIMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Set by RelayMessage to the monitor used. Null if the machine is used directly without a monitor.
    /// </summary>
    [NonSerialized]
    public EntityUid? Monitor;
}
