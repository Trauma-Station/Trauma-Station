using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Medical.Hypoport;

/// <summary>
/// Prevents a hypoport's usage if the user trying to inject is missing access on this entity's <c>AccessReader</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HypoportAccessComponent : Component;
