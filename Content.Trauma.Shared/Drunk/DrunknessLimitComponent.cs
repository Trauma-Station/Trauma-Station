using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Drunk;

/// <summary>
/// Limits drunkness for this entity to the MaxDrunkTime cvar
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DrunknessLimitComponent : Component;
