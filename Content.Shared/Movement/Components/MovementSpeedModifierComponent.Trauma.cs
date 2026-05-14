namespace Content.Shared.Movement.Components;

public sealed partial class MovementSpeedModifierComponent
{
    public static readonly Angle DefaultBackwardsAngle = Angle.FromDegrees(105);

    /// <summary>
    /// Trying to move at at least this angle from your current direction forces you to walk slowly instead of run.
    /// The cone of "forward" is double this angle.
    /// I.e. no moonwalking at full speed.
    /// </summary>
    [DataField]
    public Angle BackwardsAngle = DefaultBackwardsAngle;
}
