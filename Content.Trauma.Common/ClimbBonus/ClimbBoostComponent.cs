using Robust.Shared.GameStates;

namespace Content.Trauma.Common.ClimbBonus;

/// <summary>
/// Reduces the amount of time to climb onto something
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClimbBoostComponent : Component
{
    /// <summary>
    /// The number to divide the normal do-after time by
    /// </summary>
    [DataField]
    public float Coefficient = 1.5f;
}
