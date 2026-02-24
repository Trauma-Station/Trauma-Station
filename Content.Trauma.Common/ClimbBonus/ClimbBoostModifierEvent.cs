namespace Content.Trauma.Common.ClimbBonus;

/// <summary>
/// Raised on the target after AttemptClimbEvent divides the do-after time by the coefficient
/// </summary>
/// <param name="user"></param>
/// <param name="target"></param>
/// <param name="coefficient"></param>
[ByRefEvent]
public sealed class ClimbBoostModifierEvent(EntityUid user, EntityUid target, float? coefficient) : EntityEventArgs
{
    public EntityUid User { get; set; } = user;
    public EntityUid Target { get; set; } = target;
    public float? Coefficient { get; set; } = coefficient;
}
