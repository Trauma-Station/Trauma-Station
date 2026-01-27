using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Whatever has this component can be dragged by other entities
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BuckleableComponent : Component;
