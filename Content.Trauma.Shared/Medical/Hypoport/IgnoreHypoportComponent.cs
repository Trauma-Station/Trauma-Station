using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Medical.Hypoport;

/// <summary>
/// Component for a hypospray that makes it ignore hypoport and grab logic.
/// Only use this for needles using <c>HyposprayComponent</c> like epipens, or admeme items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreHypoportComponent : Component;
