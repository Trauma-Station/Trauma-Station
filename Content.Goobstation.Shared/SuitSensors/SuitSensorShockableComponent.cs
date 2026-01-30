using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.SuitSensors;

/// <summary>
/// If on an entity that has SuitSensorComponent, and the entity that wears it gets electrocuted, the sensors will change.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SuitSensorShockableComponent : Component;
