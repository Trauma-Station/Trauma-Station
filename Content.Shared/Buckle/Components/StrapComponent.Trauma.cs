namespace Content.Shared.Buckle.Components;

public sealed partial class StrapComponent
{
    /// <summary>
    /// Whether to add a verb to buckle things to this entity.
    /// </summary>
    [DataField]
    public bool AddBuckleVerb = true;

    /// <summary>
    /// add so can block unbuckeling of vehicle drivers
    /// </summary>
    [DataField]
    public bool AllowOthersToUnbuckle = true;

    /// <summary>
    /// Whether to block movement if buckled.
    /// For use with other components that might want the buckled entity to still be able to move.
    /// </summary>
    [DataField]
    public bool BlockMovement = true;

    /// <summary>
    /// Length of the doafter for unbuckling yourself.
    /// </summary>
    [DataField]
    public TimeSpan SelfUnBuckleDelay = TimeSpan.Zero;
}
