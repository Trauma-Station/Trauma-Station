using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Fire;

/// <summary>
/// Disables damage directly caused by fire, but not any temperature changes.
/// Does not apply if worn e.g. a FireImmunity mouse on your head does not make you fire immune.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FireImmunityComponent : Component;
