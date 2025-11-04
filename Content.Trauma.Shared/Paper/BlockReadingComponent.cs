using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Paper;

/// <summary>
/// Prevents this entity from opening the paper UI.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockReadingComponent : Component;
