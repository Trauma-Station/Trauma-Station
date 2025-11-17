using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Emp;

/// <summary>
/// Makes an entity immune to EMPs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmpImmuneComponent : Component;
