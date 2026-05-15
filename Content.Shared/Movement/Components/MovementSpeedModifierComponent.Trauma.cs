namespace Content.Shared.Movement.Components;

public sealed partial class MovementSpeedModifierComponent
{
    public static readonly Angle DefaultBackwardsAngle = Angle.FromDegrees(105);
    public const float DefaultBackwardsSpeed = 0.75f;

    /// <summary>
    /// Trying to move at at least this angle from your current direction forces you to walk slower.
    /// The cone of "forward" is double this angle.
    /// I.e. no moonwalking at full speed.
    /// </summary>
    [DataField]
    public Angle BackwardsAngle = DefaultBackwardsAngle;

    /// <summary>
    /// Multiplies your speed when walking backwards.
    /// </summary>
    [DataField]
    public float BackwardsSpeed = DefaultBackwardsSpeed;
}
